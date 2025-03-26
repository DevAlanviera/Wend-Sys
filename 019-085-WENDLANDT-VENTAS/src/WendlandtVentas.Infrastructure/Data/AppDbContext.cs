using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Web.Models.ProductViewModels;


//WendlandtVentas.Infraestructure.Data

namespace WendlandtVentas.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly ILogBookService _logBookService;


        public AppDbContext(DbContextOptions<AppDbContext> options, IDomainEventDispatcher dispatcher, ILogBookService logBookService)
            : base(options)
        {
            _dispatcher = dispatcher;
            _logBookService = logBookService;
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<ClientPromotion> ClientPromotions { get; set; }
        public DbSet<Movement> Movements { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<NotificationUser> NotificationUser { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderProduct> OrderProducts { get; set; }
        public DbSet<OrderPromotion> OrderPromotions { get; set; }
        public DbSet<OrderPromotionProduct> OrderPromotionProducts { get; set; }
        public DbSet<Presentation> Presentations { get; set; }
        public DbSet<PresentationPromotion> PresentationPromotions { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductPresentation> ProductPresentations { get; set; }
        public DbSet<Promotion> Promotion { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Address> Addresses { get; set; }

        //Referencia de la clase bitacora
        public DbSet<Bitacora> Bitacora { get; set; }

        // DbSet para la tabla PreciosEspeciales
        public DbSet<PrecioEspecial> PreciosEspeciales { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            builder.Entity<Client>(ConfigureExecution);
            builder.Entity<ClientPromotion>(ConfigureExecution);
            builder.Entity<Movement>(ConfigureExecution);
            builder.Entity<Notification>(ConfigureExecution);
            builder.Entity<NotificationUser>(ConfigureExecution);
            builder.Entity<Order>(ConfigureExecution);
            builder.Entity<OrderProduct>(ConfigureExecution);
            builder.Entity<OrderPromotion>(ConfigureExecution);
            builder.Entity<OrderPromotionProduct>(ConfigureExecution);
            builder.Entity<Presentation>(ConfigureExecution);
            builder.Entity<Product>(ConfigureExecution);
            builder.Entity<ProductPresentation>(ConfigureExecution);
            builder.Entity<PresentationPromotion>(ConfigureExecution);
            builder.Entity<Promotion>(ConfigureExecution);
            builder.Entity<State>(ConfigureExecution);
            builder.Entity<Bitacora>(ConfigureExecution);
            builder.Entity<PrecioEspecial>(ConfigureExecution);
        }

        private void ConfigureExecution(EntityTypeBuilder<PrecioEspecial> builder)
        {
            // Definir la clave primaria compuesta
            builder.HasKey(pe => new { pe.ClienteId, pe.ProductoId });

            // Relación con Cliente
            builder.HasOne(pe => pe.Cliente)
                .WithMany(c => c.PreciosEspeciales) // Si Cliente tiene una colección de PreciosEspeciales
                .HasForeignKey(pe => pe.ClienteId)
                .OnDelete(DeleteBehavior.Cascade); // Opcional: Define el comportamiento al eliminar

            // Relación con Producto
            builder.HasOne(pe => pe.Producto)
                .WithMany(p => p.PreciosEspeciales) // Si Producto tiene una colección de PreciosEspeciales
                .HasForeignKey(pe => pe.ProductoId)
                .OnDelete(DeleteBehavior.Cascade); // Opcional: Define el comportamiento al eliminar

            // Configuración del campo Precio
            builder.Property(pe => pe.Precio)
                .HasColumnType("decimal(18, 2)") // Tipo de dato en la base de datos
                .IsRequired(); // Campo obligatorio
        }

        private void ConfigureExecution(EntityTypeBuilder<Client> builder)
        {
            // aqui se definen las reglas de los campos de la tabla en la BD como los campos que son requeridos,
            // el tamaño del tipo elegido (ej. varchar[200]), el tipo de relación entre tablas, etc.

            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();
            builder.Property(c => c.CreditDays)
                .HasDefaultValue(15);
        }

        private void ConfigureExecution(EntityTypeBuilder<ClientPromotion> builder)
        {
            builder.HasKey(cp => new { cp.ClientId, cp.PromotionId });
            builder.HasOne(cp => cp.Client)
                .WithMany(c => c.ClientPromotions)
                .HasForeignKey(cp => cp.ClientId);
            builder.HasOne(cp => cp.Promotion)
                .WithMany(pr => pr.ClientPromotions)
                .HasForeignKey(cp => cp.PromotionId);
        }

        private void ConfigureExecution(EntityTypeBuilder<Movement> builder)
        {
            builder.Property(c => c.UserId)
                .IsRequired();
            builder.Property(c => c.Quantity)
                .IsRequired();
            builder.Property(c => c.QuantityCurrent)
                .IsRequired();
            builder.Property(c => c.QuantityOld)
                .IsRequired();
        }

    private void ConfigureExecution(EntityTypeBuilder<Bitacora> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Usuario)
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(b => b.Fecha_modificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne<Order>()
            .WithMany() // Una Orden puede tener muchas entradas en Bitacora
            .HasForeignKey(b => b.Registro_id);
        }

        private void ConfigureExecution(EntityTypeBuilder<Notification> builder)
        {
            builder.Property(c => c.Title)
                .IsRequired();
            builder.Property(c => c.Message)
               .IsRequired();
        }

        private void ConfigureExecution(EntityTypeBuilder<NotificationUser> builder)
        {
            builder.Property(c => c.NotificationId)
                .IsRequired();
            builder.Property(c => c.UserId)
                .IsRequired();
        }

        private void ConfigureExecution(EntityTypeBuilder<Order> builder)
        {
            builder.Property(c => c.Discount)
                .HasColumnType("decimal(18,2)");
            builder.Property(c => c.SubTotal)
                .HasColumnType("decimal(18,2)");
            builder.Property(c => c.Total)
                .HasColumnType("decimal(18,2)");
        }


        private void ConfigureExecution(EntityTypeBuilder<OrderProduct> builder)
        {
            builder.Property(c => c.OrderId)
                .IsRequired();
            builder.Property(c => c.ProductPresentationId)
                .IsRequired();
            builder.Property(c => c.Quantity)
                .IsRequired();

        }

        private void ConfigureExecution(EntityTypeBuilder<OrderPromotion> builder)
        {
            builder.Property(c => c.OrderId)
                .IsRequired();
            builder.Property(c => c.PromotionId)
                .IsRequired();
        }

        private void ConfigureExecution(EntityTypeBuilder<OrderPromotionProduct> builder)
        {
            builder.Property(c => c.OrderPromotionId)
                .IsRequired();
            builder.Property(c => c.ProductPresentationId)
                .IsRequired();
            builder.Property(c => c.Quantity)
                .IsRequired();
        }

        private void ConfigureExecution(EntityTypeBuilder<Presentation> builder)
        {
            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();
            builder.Property(c => c.Liters)
                .IsRequired();
        }

        private void ConfigureExecution(EntityTypeBuilder<PresentationPromotion> builder)
        {
            builder.HasKey(pp => new { pp.PresentationId, pp.PromotionId });
            builder.HasOne(pp => pp.Presentation)
                .WithMany(pre => pre.PresentationPromotions)
                .HasForeignKey(pp => pp.PresentationId);
            builder.HasOne(pp => pp.Promotion)
                .WithMany(pro => pro.PresentationPromotions)
                .HasForeignKey(pp => pp.PromotionId);

        }

        private void ConfigureExecution(EntityTypeBuilder<Product> builder)
        {
            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();
        }

        private void ConfigureExecution(EntityTypeBuilder<ProductPresentation> builder)
        {
            builder.Property(c => c.Price)
               .HasColumnType("decimal(18,2)");
            builder.Property(c => c.PriceUsd)
               .HasColumnType("decimal(18,2)");
        }

        private void ConfigureExecution(EntityTypeBuilder<Promotion> builder)
        {
            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();
            builder.Property(c => c.Buy)
                .IsRequired();
            builder.Property(c => c.Present)
                .IsRequired();
            builder.Property(c => c.Discount)
                .IsRequired();
        }

        private void ConfigureExecution(EntityTypeBuilder<State> builder)
        {
            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            // actualiza la fecha de los elementos que se actualizan
            var modifiedEntities = ChangeTracker.Entries<BaseEntity>().Where(c => c.State == EntityState.Modified);

            foreach (var entity in modifiedEntities)
                entity.Entity.UpdatedAt = DateTime.UtcNow;

            //try
            //{
            //    var res = await _logBookService.CreateLogBook(ChangeTracker);
            //    await _logBookService.SendPost(res);
            //}
            //catch (Exception)
            //{

            //}
            int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // ignore events if no dispatcher provided
            if (_dispatcher == null) return result;

            //dispatch events only if save was successful
            var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
                .Select(e => e.Entity)
                .Where(e => e.Events.Any())
                .ToArray();

            foreach (var entity in entitiesWithEvents)
            {
                var events = entity.Events.ToArray();
                entity.Events.Clear();
                foreach (var domainEvent in events)
                {
                    await _dispatcher.Dispatch(domainEvent).ConfigureAwait(false);
                }
            }

            return result;
        }

        public override int SaveChanges()
        {
            return SaveChangesAsync().GetAwaiter().GetResult();
        }
    }
}