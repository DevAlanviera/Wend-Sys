using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.MovementSpecifications
{
    public class SingleByPresentationIdSpecification : BaseSpecification<Movement>
    {
        public SingleByPresentationIdSpecification(int productPresentationId) : base(c => c.ProductPresentationId == productPresentationId)
        {
            AddInclude(c => c.ProductPresentation);
        }
    }
}
