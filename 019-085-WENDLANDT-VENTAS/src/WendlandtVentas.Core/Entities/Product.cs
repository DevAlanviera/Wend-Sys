using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Web.Models.ProductViewModels;

namespace WendlandtVentas.Core.Entities
{
    public class Product : BaseEntity, IAggregateRoot
    {
        public string Name { get; private set; }
        public Distinction Distinction { get; private set; }
        public string Season { get; private set; }
        public ICollection<ProductPresentation> ProductPresentations { get; private set; }

        // Propiedad de navegación para PrecioEspecial
        public virtual ICollection<PrecioEspecial> PreciosEspeciales { get; set; }

        // --- NUEVA PROPIEDAD PARA UNIFICAR INVENTARIO ---
        // Si es null, el producto gestiona su propio inventario (es un BC).
        // Si tiene valor, el inventario se descuenta de ese ID.
        public int? InventorySourceId { get; private set; }

        public virtual Product InventorySource { get; private set; }

        public bool IsBundle { get; set; } // Identifica si es un paquete (como el 12-pack)
        // Esto le dice a EF y a C# que un Producto puede ser un Pack con muchos componentes
        public virtual ICollection<ProductBundleComponent> BundleComponents { get; set; } = new List<ProductBundleComponent>();

        public Product()
        {
            ProductPresentations = new List<ProductPresentation>();
        }
        public Product(string name, Distinction distinction, string season, int? inventorySourceId = null)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.Negative((int)distinction, nameof(distinction));

            Name = name;
            Distinction = distinction;
            Season = distinction == Distinction.Season ? season : string.Empty;
            InventorySourceId = inventorySourceId; // Asignación inicial
        }

        // Nos dice de qué ID de producto vamos a descontar realmente
        public int EffectiveInventoryId => InventorySourceId ?? Id;

        public void Edit(string name, Distinction distinction, string season, int? inventorySourceId = null)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.Negative((int)distinction, nameof(distinction));

            Name = name;
            Distinction = distinction;
            Season = distinction == Distinction.Season ? season : string.Empty;
            InventorySourceId = inventorySourceId;
        }

        // Método opcional por si solo quieres cambiar el vínculo sin editar todo el producto
        public void SetInventorySource(int? inventorySourceId)
        {
            // Podrías agregar una validación para que un producto no sea fuente de sí mismo
            if (inventorySourceId.HasValue && inventorySourceId.Value == this.Id)
                throw new ArgumentException("Un producto no puede ser su propia fuente de inventario.");

            InventorySourceId = inventorySourceId;
        }
    }
}