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

            // 1. Validamos si el cliente es nulo (caso de prospecto/cotización)
            if (client == null || client.Id == 0)
            {
                // Solo devolvemos promociones que sean de tipo General
                AppendCriteria(c => c.Type == PromotionType.General, true);
            }
            else
            {
                // 2. Lógica para clientes reales
                if (client.Classification != null && client.Classification.HasValue)
                {
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
                        default:
                            AppendCriteria(c => c.Type == PromotionType.General || (c.Type == PromotionType.Clients && c.ClientPromotions.Any(d => !d.IsDeleted && d.ClientId == client.Id)), true);
                            break;
                    }
                }
                else
                {
                    // Cliente real pero sin clasificación asignada
                    AppendCriteria(c => c.Type == PromotionType.General || (c.Type == PromotionType.Clients && c.ClientPromotions.Any(d => !d.IsDeleted && d.ClientId == client.Id)), true);
                }
            }

            // Siempre filtrar por activas y no eliminadas
            AppendCriteria(c => !c.IsDeleted && c.IsActive, true);
        }
    }
}