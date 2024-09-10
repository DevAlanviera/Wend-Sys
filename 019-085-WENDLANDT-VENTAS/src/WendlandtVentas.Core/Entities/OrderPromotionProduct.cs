using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;

namespace WendlandtVentas.Core.Entities
{
    public class OrderPromotionProduct : BaseEntity, IAggregateRoot
    {
        public int OrderPromotionId { get; private set; }
        public OrderPromotion OrderPromotion { get; private set; }
        public int ProductPresentationId { get; private set; }
        public ProductPresentation ProductPresentation { get; private set; }
        public int Quantity { get; private set; }

        public decimal Discount { get; set; }
        public OrderPromotionProduct()
        {
        }
        public OrderPromotionProduct(ProductPresentation productPresentation, int quantity)
        {
            Guard.Against.Null(productPresentation, nameof(productPresentation));
            Guard.Against.NegativeOrZero(quantity, nameof(quantity));

            ProductPresentation = productPresentation;
            ProductPresentationId = productPresentation.Id;
            Quantity = quantity;
            Discount = (decimal)(productPresentation.Price * quantity);
        }
    }
}