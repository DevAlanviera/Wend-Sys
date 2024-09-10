using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;

namespace WendlandtVentas.Core.Entities
{
    public class PresentationPromotion : BaseEntity, IAggregateRoot
    {
        public int PresentationId { get; private set; }
        public Presentation Presentation { get; private set; }
        public int PromotionId { get; private set; }
        public Promotion Promotion { get; private set; }

        public PresentationPromotion(int presentationId)
        {
            Guard.Against.NegativeOrZero(presentationId, nameof(presentationId));

            PresentationId = presentationId;
        }
    }
}
