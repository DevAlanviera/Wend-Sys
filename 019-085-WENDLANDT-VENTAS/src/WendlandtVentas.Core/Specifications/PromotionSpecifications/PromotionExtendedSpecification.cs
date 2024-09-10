using WendlandtVentas.Core.Entities;
namespace WendlandtVentas.Core.Specifications.PromotionSpecifications
{
    public class PromotionExtendedSpecification : BaseSpecification<Promotion>
    {
        public PromotionExtendedSpecification(int promotionId) : base(c => c.Id == promotionId)
        {
            AddInclude($"{nameof(Promotion.ClientPromotions)}.{nameof(ClientPromotion.Client)}");
            AddInclude($"{nameof(Promotion.PresentationPromotions)}.{nameof(PresentationPromotion.Presentation)}");
        }

        public PromotionExtendedSpecification() : base(c => true)
        {
            AddInclude($"{nameof(Promotion.ClientPromotions)}.{nameof(ClientPromotion.Client)}");
            AddInclude($"{nameof(Promotion.PresentationPromotions)}.{nameof(PresentationPromotion.Presentation)}"); ;
        }
    }
}