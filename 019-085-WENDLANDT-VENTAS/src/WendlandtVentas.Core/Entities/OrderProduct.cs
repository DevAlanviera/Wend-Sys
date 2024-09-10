using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;

namespace WendlandtVentas.Core.Entities
{
    public class OrderProduct : BaseEntity, IAggregateRoot
    {
        public int OrderId { get; private set; }
        public Order Order { get; private set; }
        public int ProductPresentationId { get; private set; }
        public ProductPresentation ProductPresentation { get; private set; }
        public int Quantity { get; private set; }
        public bool IsPresent { get; private set; }
        public decimal Price { get; private set; }

        public OrderProduct() { }

        public OrderProduct(ProductPresentation productPresentation, int quantity, bool isPresent, decimal price)
        {
            Guard.Against.Null(productPresentation, nameof(productPresentation));
            Guard.Against.OutOfRange(quantity, nameof(quantity), 1, int.MaxValue);

            ProductPresentation = productPresentation;
            ProductPresentationId = productPresentation.Id;
            Quantity = quantity;
            IsPresent = isPresent;
            Price = price;
        }

        public void EditPrice(decimal price)
        {
            Price = price;
        }
    }
}