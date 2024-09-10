using Microsoft.EntityFrameworkCore.Internal;
using System.Linq;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Core.Specifications.PromotionSpecifications
{
    public class PromotionByClientSpecification : BaseSpecification<Promotion>
    {
        public PromotionByClientSpecification(Client client) : base(c => true)
        {
            AddInclude($"{nameof(Promotion.ClientPromotions)}.{nameof(ClientPromotion.Client)}");
            AddInclude($"{nameof(Promotion.PresentationPromotions)}.{nameof(PresentationPromotion.Presentation)}");

            //AppendCriteria(c => c.Type == PromotionType.General || (c.Type == PromotionType.Clients && c.ClientPromotions.Any(d => !d.IsDeleted && d.ClientId == client.Id)), true);

            if (client.Classification != null && client.Classification.HasValue)
                switch (client.Classification.Value)
                {
                    case Classification.Gold:
                        AppendCriteria(c => c.Type == PromotionType.General || (c.Type == PromotionType.Clients && c.ClientPromotions.Any(d => !d.IsDeleted && d.ClientId == client.Id) || c.Type == PromotionType.Classification), true);
                        break;
                    case Classification.Silver:
                        AppendCriteria(c => c.Type == PromotionType.General || (c.Type == PromotionType.Clients && c.ClientPromotions.Any(d => !d.IsDeleted && d.ClientId == client.Id) || (c.Type == PromotionType.Classification && c.Classification != Classification.Gold)), true);
                        break;
                    case Classification.Bronze:
                        AppendCriteria(c => c.Type == PromotionType.General || (c.Type == PromotionType.Clients && c.ClientPromotions.Any(d => !d.IsDeleted && d.ClientId == client.Id) || (c.Type == PromotionType.Classification && c.Classification == Classification.Bronze)), true);
                        break;
                }
            else
                AppendCriteria(c => c.Type == PromotionType.General || (c.Type == PromotionType.Clients && c.ClientPromotions.Any(d => !d.IsDeleted && d.ClientId == client.Id) && c.Type != PromotionType.Classification), true);


            AppendCriteria(c => !c.IsDeleted && c.IsActive, true);
        }
    }
}