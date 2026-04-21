using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.DTO;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using WendlandtVentas.Core.Specifications.ProductSpecifications;

namespace WendlandtVentas.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAsyncRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<InventoryService> _logger; // <-- 1. Declarar
        private readonly IEmailSender _emailSender;
        private readonly IExcelReadService _excelReaderService;

        public InventoryService(UserManager<ApplicationUser> userManager, IAsyncRepository repository, ILogger<InventoryService> logger, IEmailSender emailSender, IExcelReadService excelReaderService)
        {
            _userManager = userManager;
            _repository = repository;
            _logger = logger;
            _emailSender = emailSender;
            _excelReaderService = excelReaderService;
        }

        public async Task<Response> OrderDiscount(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(email);
                var movements = new List<Movement>();

                foreach (var item in productsPresentations)
                {

                    // 1. Obtenemos los datos básicos de la presentación que se quiere vender
                    // Usamos GetQueryable para traer el Product e InventorySourceId sin una Spec nueva
                    var current = await _repository.GetQueryable<ProductPresentation>()
                        .Include(p => p.Product)
                        .FirstOrDefaultAsync(p => p.Id == item.Id);

                    if (current == null) continue;

                    // 2. Determinamos de qué ID vamos a descontar realmente
                    int presentationIdADescontar = item.Id;

                    // ¿Es un producto ligado a un maestro (B.C.)?
                    if (current.Product.InventorySourceId.HasValue)
                    {
                        // Buscamos la presentación gemela en el maestro
                        // (Ejemplo: Si el nacional es "Botella", buscamos la "Botella" del B.C.)
                        var masterId = await _repository.GetQueryable<ProductPresentation>()
                            .Where(p => p.ProductId == current.Product.InventorySourceId &&
                                        p.PresentationId == current.PresentationId)
                            .Select(p => p.Id)
                            .FirstOrDefaultAsync();

                        if (masterId > 0)
                        {
                            presentationIdADescontar = masterId;
                        }
                    }

                    // --- CORRECCIÓN AQUÍ: Usamos presentationIdADescontar ---
                    var batches = await _repository.GetQueryable<Batch>()
                        .Where(b => b.ProductPresentationId == presentationIdADescontar && b.CurrentQuantity > 0 && b.IsActive)
                        .OrderBy(b => b.ExpiryDate)
                        .ThenBy(b => b.Id)
                        .ToListAsync();

                    // --- MEJORA: Validar stock total antes de empezar ---
                    int totalDisponible = batches.Sum(b => b.CurrentQuantity);
                    if (totalDisponible < item.Quantity)
                    {
                        return new Response(false, $"Stock insuficiente para el producto ID {item.Id}. Requerido: {item.Quantity}, Disponible: {totalDisponible}");
                    }

                    decimal pendientePorDescontar = item.Quantity;

                    foreach (var batch in batches)
                    {
                        // Si ya no hay nada que descontar, salimos del bucle
                        if (pendientePorDescontar <= 0) break;

                        // IMPORTANTE: cantidadATomar debe ser el mínimo entre lo que tiene el lote 
                        // y lo que nos falta por surtir
                        int cantidadATomar = Math.Min(batch.CurrentQuantity, (int)pendientePorDescontar);

                        if (cantidadATomar <= 0) continue; // Si este lote está vacío por error, saltar al siguiente

                        var movement = new Movement(
                            item.Id,
                            cantidadATomar,
                            Operation.Out,
                            batch.CurrentQuantity,
                            $"Pedido {orderId}",
                            user.Id
                        );
                        movement.BatchId = batch.Id;
                        movements.Add(movement);

                        // --- EL PUNTO CRÍTICO ---
                        batch.CurrentQuantity -= cantidadATomar;

                        // Si el lote llegó a 0, lo desactivamos
                        if (batch.CurrentQuantity <= 0)
                        {
                            batch.CurrentQuantity = 0; // Aseguramos que no sea negativo
                            batch.IsActive = false;
                        }

                        // Restamos lo que ya tomamos de la deuda total del pedido
                        pendientePorDescontar -= cantidadATomar;

                        // Guardamos los cambios del lote actual antes de pasar al siguiente
                        await _repository.UpdateAsync(batch);
                    }

                    // Opcional: Si después de recorrer lotes aún falta cantidad, 
                    // significa que vendiste algo que no tienes en batches.
                }

                await _repository.AddRangeAsync(movements);
                return new Response(true, "Descuento de inventario por lotes realizado");
            }
            catch (Exception e)
            {
                return new Response(false, "No se pudo realizar el descuento por lotes");
            }
        }

        public async Task<Response> OrderReturn(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(email);
                var movements = new List<Movement>();

                foreach (var item in productsPresentations)
                {
                    // 1. Obtenemos la presentación que se está devolviendo
                    var current = await _repository.GetQueryable<ProductPresentation>()
                        .Include(p => p.Product)
                        .FirstOrDefaultAsync(p => p.Id == item.Id);

                    if (current == null) continue;

                    // 2. Determinamos a qué ID debe regresar el stock realmente
                    int presentationIdARetornar = item.Id;

                    if (current.Product.InventorySourceId.HasValue)
                    {
                        // Buscamos la presentación gemela en el maestro (B.C.)
                        var masterId = await _repository.GetQueryable<ProductPresentation>()
                            .Where(p => p.ProductId == current.Product.InventorySourceId &&
                                        p.PresentationId == current.PresentationId)
                            .Select(p => p.Id)
                            .FirstOrDefaultAsync();

                        if (masterId > 0)
                        {
                            presentationIdARetornar = masterId;
                        }
                    }

                    // 3. Buscamos el lote activo más reciente del ID EFECTIVO
                    // (Queremos sumarlo al lote que caduque más tarde para que se use después)
                    var batch = await _repository.GetQueryable<Batch>()
                        .Where(b => b.ProductPresentationId == presentationIdARetornar && b.IsActive)
                        .OrderByDescending(b => b.ExpiryDate)
                        .FirstOrDefaultAsync();

                    if (batch != null)
                    {
                        // Aumentamos el stock del lote (del Maestro o del Propio)
                        batch.CurrentQuantity += (int)item.Quantity;

                        // Registramos el movimiento de ENTRADA
                        var movement = new Movement(
                            item.Id, // Registramos que regresó el "NACIONAL"
                            item.Quantity,
                            Operation.In, // ENTRADA
                            batch.CurrentQuantity,
                            $"Devolución Pedido {orderId} (Retornado a Maestro: {presentationIdARetornar})",
                            user.Id
                        );
                        movement.BatchId = batch.Id;
                        movements.Add(movement);

                        // Persistimos el cambio en el lote
                        await _repository.UpdateAsync(batch);
                    }
                    else
                    {
                        _logger.LogWarning($"No hay lotes activos para el producto ID {presentationIdARetornar}. " +
                                           $"No se pudo procesar la devolución automática de la Orden {orderId}.");
                        // NOTA: Si no hay lotes, la cerveza se queda en el "limbo" de la auditoría. 
                        // Podrías crear un lote nuevo aquí si fuera necesario.
                    }
                }

                if (movements.Any())
                {
                    await _repository.AddRangeAsync(movements);
                }

                return new Response(true, "El inventario ha sido actualizado y unificado correctamente.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al procesar devolución");
                return new Response(false, "Error crítico al retornar productos al inventario.");
            }
        }

        public async Task<int> GetAvailableStock(int productPresentationId)
        {
           

            // 1. Buscamos la presentación que nos pasaron
            var presentation = await _repository.GetQueryable<ProductPresentation>()
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == productPresentationId);

            if (presentation == null) return 0;

            // ID por el cual sumaremos los lotes (por defecto el que recibimos)
            int idParaSumarLotes = productPresentationId;

            // 2. LÓGICA DE UNIFICACIÓN:
            // Si el producto tiene un InventorySourceId, significa que es una variante (NAL, FG, etc.)
            if (presentation.Product.InventorySourceId.HasValue)
            {
                // Buscamos la presentación EQUIVALENTE en el producto maestro (B.C.)
                // Queremos el mismo envase (ej: Barril 30L) pero del Producto Maestro
                var masterPresentation = await _repository.GetQueryable<ProductPresentation>()
                    .FirstOrDefaultAsync(pp => pp.ProductId == presentation.Product.InventorySourceId
                                           && pp.PresentationId == presentation.PresentationId);

                if (masterPresentation != null)
                {
                    idParaSumarLotes = masterPresentation.Id;
                }
            }

            // 3. Sumamos los lotes activos del ID final
            return await _repository.GetQueryable<Batch>()
                .Where(b => b.ProductPresentationId == idParaSumarLotes && b.IsActive)
                .SumAsync(b => b.CurrentQuantity);
        }

        public async Task ReverseOrderInventory(int orderId)
        {
            var movements = await _repository.GetQueryable<Movement>()
        .Where(m => m.Comment.Contains($"(Orden {orderId})") || m.Comment.Contains($"Pedido {orderId}"))
        .ToListAsync();

            foreach (var mov in movements)
            {
                if (mov.BatchId.HasValue)
                {
                    var batch = await _repository.GetByIdAsync<Batch>(mov.BatchId.Value);
                    if (batch != null)
                    {
                        // REVERSA: Si salió, suma. Si entró, resta.
                        if (mov.Operation == Operation.Out) batch.CurrentQuantity += (int)mov.Quantity;
                        else batch.CurrentQuantity -= (int)mov.Quantity;

                        await _repository.UpdateAsync(batch);
                    }
                }

                // BORRADO INDIVIDUAL: Usamos el método que ya conoce tu repositorio
                await _repository.DeleteAsync(mov);
            }
        }

        public async Task VerificarStockBajoAsync(ProductPresentation p)
        {
            // 2. ¡BORRA O COMENTA la línea del repository! Ya no la necesitas.
            // var p = await _repository.GetAsync(spec); 

            if (p == null || p.Presentation == null) return;

            // 3. Usa directamente 'p' que viene por parámetro
            int stockActual = await GetAvailableStock(p.Id);
            double litros = p.Presentation.Liters;

            bool requiereAlerta = (litros <= 1) ? (stockActual <= 2400) : (stockActual <= 20);

            if (requiereAlerta)
            {
                await EnviarCorreoAlertaStockAsync(p, stockActual);
            }
        }

        public async Task<List<ProductPresentationQuantity>> DescomponerBundlesAsync(IEnumerable<ProductPresentationQuantity> items)
        {
            var result = new List<ProductPresentationQuantity>();
            const int BOTELLA_PRESENTATION_ID = 3;

            foreach (var item in items)
            {
                var presentation = await _repository.GetQueryable<ProductPresentation>()
                    .Include(pp => pp.Product)
                    .FirstOrDefaultAsync(pp => pp.Id == item.Id);

                if (presentation == null)
                {
                    result.Add(item);
                    continue;
                }

                // 🔥 Verificar si es bundle
                if (presentation.Product != null && presentation.Product.IsBundle)
                {
                    // Consulta directa a ProductBundleComponent
                    var bundleComponents = await _repository.GetQueryable<ProductBundleComponent>()
                        .Include(bc => bc.ComponentProduct)
                            .ThenInclude(cp => cp.ProductPresentations)
                                .ThenInclude(pp => pp.Presentation)
                        .Where(bc => bc.BundleProductId == presentation.Product.Id && !bc.IsDeleted)
                        .ToListAsync();

                    if (!bundleComponents.Any())
                    {
                        _logger.LogWarning($"Bundle {presentation.Product.Name} no tiene componentes");
                        continue;
                    }

                    foreach (var component in bundleComponents)
                    {
                        // 🔥 CORREGIDO: Acceder correctamente a las presentaciones
                        var componentPresentation = component.ComponentProduct?.ProductPresentations
                            .FirstOrDefault(pp => !pp.IsDeleted && pp.PresentationId == BOTELLA_PRESENTATION_ID);

                        if (componentPresentation != null)
                        {
                            result.Add(new ProductPresentationQuantity
                            {
                                Id = componentPresentation.Id,
                                Quantity = component.Quantity * item.Quantity
                            });
                        }
                        else
                        {
                            _logger.LogWarning($"Componente {component.ComponentProduct?.Name} no tiene presentación Botella");
                        }
                    }
                }
                else
                {
                    // Producto normal
                    result.Add(item);
                }
            }

            // Agrupar por ID para sumar cantidades iguales
            var groupedResult = result
                .GroupBy(x => x.Id)
                .Select(g => new ProductPresentationQuantity
                {
                    Id = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            return groupedResult;
        }

        private async Task<bool> EnviarCorreoAlertaStockAsync(ProductPresentation p, int stockActual)
        {
            var asunto = $"ALERTA DE INVENTARIO BAJO: {p.Product.Name} ({p.Presentation.Name})";

            // Construimos un mensaje HTML sencillo pero claro para el equipo de producción/almacén
            var mensaje = $@"
            <html>
                <body>
                    <h2 style='color: #d9534f;'>Notificación de Stock Crítico</h2>
                    <p>El siguiente producto ha alcanzado su nivel mínimo de inventario:</p>
                    <ul>
                        <li><strong>Producto:</strong> {p.Product.Name}</li>
                        <li><strong>Presentación:</strong> {p.Presentation.Name} ({p.Presentation.Liters} L)</li>
                        <li><strong>Existencia Actual:</strong> <span style='color: red; font-size: 18px;'>{stockActual}</span></li>
                    </ul>
                    <p>Se recomienda programar producción o reabastecimiento a la brevedad.</p>
                    <hr />
                    <p><small>Este es un mensaje automático de Monobits.</small></p>
                </body>
            </html>";

            // Enviamos a un correo configurado (ej. almacen@wendlandt.com)
            var destinatarios = new[]
            {
                "raul.medina@wendlandt.com.mx",
                "francisco.hernandez@wendlandt.com.mx",
                "nestor.camacho@wendlandt.com.mx"
            };

            bool todosExitosos = true;

            foreach (var destinatario in destinatarios)
            {
                var resultado = await _emailSender.SendEmailAsync(
                    email: destinatario,
                    subject: asunto,
                    message: mensaje,
                    perfil: "Email"
                );

                if (!resultado)
                {
                    _logger.LogWarning($"No se pudo enviar correo de alerta a {destinatario}");
                    todosExitosos = false;
                }
            }
            return todosExitosos;
        }

        public async Task<List<FilaReporteInventario>> ObtenerDatosParaReporteExcelAsync()
        {
            // 1. Definimos tus cervezas de línea (tal cual están en la imagen)
            // 1. Extraemos los lotes filtrando ÚNICAMENTE por el prefijo "B.C."
            // 1. Extraemos los lotes con el nuevo filtro combinado
            var lotes = await _repository.GetQueryable<Batch>()
                .AsNoTracking()
                .Include(b => b.ProductPresentation).ThenInclude(pp => pp.Product)
                .Include(b => b.ProductPresentation).ThenInclude(pp => pp.Presentation)
                .Where(b => b.IsActive && b.CurrentQuantity > 0 && !b.ProductPresentation.Product.IsDeleted)
                // 🔥 FILTRO AMPLIADO: Empiezan con B.C. O son de Temporada
                .Where(b => b.ProductPresentation.Product.Name.StartsWith("B.C.") ||
                            b.ProductPresentation.Product.Distinction == Distinction.Season)
                .ToListAsync();

            // 2. Agrupamos por Nombre y Lote (tal cual lo tienes en tu flujo)
            var reporte = lotes.GroupBy(b => new { b.ProductPresentation.Product.Name, b.BatchNumber })
                .Select(g => new FilaReporteInventario
                {
                    Cerveza = g.Key.Name,
                    Lote = g.Key.BatchNumber,
                    Caducidad = g.Max(x => x.ExpiryDate).ToString("dd-MMM-yy"),

                    // --- DIFERENCIACIÓN DE BARRILES ---
                    Sesentas = g.Where(x => x.ProductPresentation.PresentationId == 4).Sum(x => x.CurrentQuantity), // Inox 60L
                    VeintesSteel = g.Where(x => x.ProductPresentation.PresentationId == 6).Sum(x => x.CurrentQuantity), // Inox 20L
                    VeintesPet = g.Where(x => x.ProductPresentation.PresentationId == 8).Sum(x => x.CurrentQuantity), // PET 20L

                    EsTemporada = g.Any(x => x.ProductPresentation.Product.Distinction == Distinction.Season),

                    CajaBotella = g.Where(x => x.ProductPresentation.PresentationId == 10).Sum(x => x.CurrentQuantity),
                    BotellaSuelta = g.Where(x => x.ProductPresentation.PresentationId == 3).Sum(x => x.CurrentQuantity),
                    LataSuelta = g.Where(x => x.ProductPresentation.PresentationId == 2).Sum(x => x.CurrentQuantity),
                    //CajaLata = g.Where(x => x.ProductPresentation.PresentationId == 11).Sum(x => x.CurrentQuantity),
                    //LataSuelta = g.Where(x => x.ProductPresentation.PresentationId == 2).Sum(x => x.CurrentQuantity)
                })
                // Ordenamos para que salgan en un orden consistente
                .OrderBy(x => x.Cerveza)
                .ToList();

            return reporte;
        }

        public async Task ProcesarYEnviarReporteMatutinoAsync()
        {
            
            try
            {
                // 1. Extraer los datos de la DB (el método que ya hicimos)
                var datos = await ObtenerDatosParaReporteExcelAsync();

                if (datos == null || !datos.Any())
                {
                    _logger.LogWarning("No hay inventario activo para generar el reporte matutino.");
                    return;
                }

                // 2. Generar los bytes del archivo Excel (usando tu ExcelService)
                // Nota: Si moviste el método al ExcelService, aquí lo llamas
                var excelBytes = _excelReaderService.GenerarReporteInventario(datos);

                // 3. Configurar destinatarios y enviar
                string fechaStr = DateTime.Now.ToString("dd/MM/yyyy");
                var destinatarios = new List<string> { "a.cordova.viera@gmail.com" };

                await _emailSender.SendEmailAsync(
                 email: "francisco.hernandez@wendlandt.com.mx",
                 subject: $"📦 Reporte de Inventario Físico - {fechaStr}",
                 message: "Buen día, adjunto el reporte de inventario generado automáticamente a las 9:00 AM.",
                 file: null,
                 attachmentBytes: excelBytes,
                 attachmentName: $"Inventario_{DateTime.Now:yyyyMMdd}.xlsx",
                 perfil: "Email" // O el perfil que uses para estos envíos
                );

                _logger.LogInformation("Reporte matutino enviado exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al procesar el reporte de inventario de las 9:00 AM.");
            }
        }

    }
}
