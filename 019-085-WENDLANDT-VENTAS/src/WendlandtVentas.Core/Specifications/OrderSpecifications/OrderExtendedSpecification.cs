using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.OrderExtendedSpecifications
{
    public class OrderExtendedSpecification : BaseSpecification<Order>
    {
        public OrderExtendedSpecification(int orderId) : base(c => c.Id == orderId)
        {
            AddInclude($"{nameof(Order.Client)}.{nameof(Client.State)}");
            AddInclude($"{nameof(Order.Client)}.{nameof(Client.Addresses)}");
            AddInclude($"{nameof(Order.OrderProducts)}.{nameof(OrderProduct.ProductPresentation)}.{nameof(ProductPresentation.Product)}");
            AddInclude($"{nameof(Order.OrderProducts)}.{nameof(OrderProduct.ProductPresentation)}.{nameof(ProductPresentation.Presentation)}");
            AddInclude($"{nameof(Order.OrderPromotions)}.{nameof(OrderPromotion.Promotion)}");
            AddInclude($"{nameof(Order.OrderPromotions)}.{nameof(OrderPromotion.OrderPromotionProducts)}.{nameof(OrderPromotionProduct.ProductPresentation)}.{nameof(ProductPresentation.Product)}");
            AddInclude($"{nameof(Order.OrderPromotions)}.{nameof(OrderPromotion.OrderPromotionProducts)}.{nameof(OrderPromotionProduct.ProductPresentation)}.{nameof(ProductPresentation.Presentation)}.{nameof(Presentation.PresentationPromotions)}.{nameof(PresentationPromotion.Promotion)}");
            AddInclude($"{nameof(Order.Client)}.{nameof(Client.Comment)}");
        }

        public OrderExtendedSpecification() : base(c => true)
        {
            //AddInclude($"{nameof(Order.Client)}.{nameof(Client.Addresses)}");
            AddInclude($"{nameof(Order.Client)}.{nameof(Client.State)}");
            AddInclude($"{nameof(Order.OrderProducts)}.{nameof(OrderProduct.ProductPresentation)}.{nameof(ProductPresentation.Product)}");
            AddInclude($"{nameof(Order.OrderProducts)}.{nameof(OrderProduct.ProductPresentation)}.{nameof(ProductPresentation.Presentation)}");
            AddInclude($"{nameof(Order.Client)}.{nameof(Client.Comment)}");
            //AddInclude($"{nameof(Order.OrderProducts)}.{nameof(OrderProduct.ProductPresentation)}.{nameof(ProductPresentation.Presentation)}.{nameof(Presentation.PresentationPromotions)}");
            //AddInclude($"{nameof(Order.OrderPromotions)}.{nameof(OrderPromotion.Promotion)}");
            //AddInclude($"{nameof(Order.OrderPromotions)}.{nameof(OrderPromotion.OrderPromotionProducts)}.{nameof(OrderPromotionProduct.ProductPresentation)}.{nameof(ProductPresentation.Product)}");
            //AddInclude($"{nameof(Order.OrderPromotions)}.{nameof(OrderPromotion.OrderPromotionProducts)}.{nameof(OrderPromotionProduct.ProductPresentation)}.{nameof(ProductPresentation.Presentation)}");
        }
    }
}
