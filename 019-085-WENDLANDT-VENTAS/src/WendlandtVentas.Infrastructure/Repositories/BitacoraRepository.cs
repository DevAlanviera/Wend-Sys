using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Infrastructure.Data; // Aseg�rate de tener la referencia a tu DbContext
using WendlandtVentas.Core.Models;
using System.Linq;

namespace WendlandtVentas.Infrastructure.Repositories
{
    public class BitacoraRepository : IBitacoraRepository
    {
        private readonly AppDbContext _context;

        public BitacoraRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Bitacora bitacora)
        {

          

            try
            {
                await _context.Bitacora.AddAsync(bitacora);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Registra el error o lo lanza de nuevo seg�n sea necesario
                Console.WriteLine($"Error al insertar en bit�cora: {ex.Message}");
                throw;
            }// Guarda los cambios en la base de datos
        }

        public async Task<IEnumerable<Bitacora>> GetBitacorasByOrderIdAsync(int orderId)
        {
            return await _context.Bitacora
                .Where(b => b.Registro_id == orderId)
                .ToListAsync();
        }
        public async Task AddEditLogAsync(int orderId, string userName, string Accion)
        {
            /*var logEntry = new Bitacora
            {
                Registro_id = orderId,
                Usuario = $"{userName} editó la orden con ID: {orderId}",
                FechaModificacion = DateTime.UtcNow // O usa el método que tienes para establecer la fecha
            };

            await _bitacoraRepository.AddAsync(logEntry);
      */}


    }
}