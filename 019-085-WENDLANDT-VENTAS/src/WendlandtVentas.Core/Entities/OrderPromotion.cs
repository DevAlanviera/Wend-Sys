using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace WendlandtVentas.Core.Entities
{
    public class OrderPromotion : BaseEntity,  IAggregateRoot
    {
        public int  OrderId { get; private set; }
        public Order Order { get; private set; }
        public int PromotionId { get; private set; }
        public Promotion Promotion { get; private set; }
        public decimal Discount { get; private set; }
        public ICollection<OrderPromotionProduct> OrderPromotionProducts { get; private set; } = new List<OrderPromotionProduct>();
        public OrderPromotion()
        {
        }
        public OrderPromotion(int promotionId, IEnumerable<OrderPromotionProduct> orderPromotionProducts)
        {
            Guard.Against.NegativeOrZero(promotionId, nameof(promotionId));
            Guard.Against.Null(orderPromotionProducts, nameof(orderPromotionProducts));

            PromotionId = promotionId;
            OrderPromotionProducts = orderPromotionProducts.ToList();
            Discount = orderPromotionProducts.Sum(c => c.Discount);
        }
    }
}