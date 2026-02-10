using Ardalis.GuardClauses;
using Humanizer;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;

namespace WendlandtVentas.Core.Entities
{
    public class ProductPresentation : BaseEntity, IAggregateRoot
    {
        public int ProductId { get; private set; }
        public Product Product { get; private set; }
        public int PresentationId { get; private set; }
        public Presentation Presentation { get; private set; }
        public decimal Price { get; private set; }
        public decimal PriceUsd { get; private set; }
        public decimal Weight { get; private set; }
        public ICollection<Movement> Movements { get; set; } = new List<Movement>();

        // Agrega esta línea para habilitar la relación
        public virtual ICollection<Batch> Batches { get; set; } = new HashSet<Batch>();

        public ProductPresentation() { }

        public ProductPresentation(int productId, int presentationId, decimal price, decimal priceUsd, decimal weight)
        {
            Guard.Against.OutOfRange(productId, nameof(productId), 1, int.MaxValue);
            Guard.Against.OutOfRange(presentationId, nameof(presentationId), 1, int.MaxValue);

            ProductId = productId;
            PresentationId = presentationId;
            Price = price;
            PriceUsd = priceUsd;
            Weight = weight;
        }

        public void EditPrice(decimal price)
        {
            Price = price;
        }

        public void EditPriceUsd(decimal priceUsd)
        {
            PriceUsd = priceUsd;
        }

        public void EditWeight(decimal weight)
        {
            Weight = weight;
        }

       // public string NameExtended() =>
    //$"{Product?.Name ?? "Sin producto"} - {Product?.Distinction.Humanize() ?? ""} ({Presentation?.Name ?? "Sin presentación"} {Presentation?.Liters.ToString() ?? ""} lts.)";

        public string NameExtended() => $"{Product?.Name} - {Product.Distinction.Humanize()} ({Presentation?.Name} {Presentation?.Liters} lts.)";
    }
}