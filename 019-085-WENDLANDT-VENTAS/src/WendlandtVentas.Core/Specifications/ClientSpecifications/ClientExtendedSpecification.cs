using WendlandtVentas.Core.Entities;
namespace WendlandtVentas.Core.Specifications.ClientSpecifications
{
    public class ClientExtendedSpecification : BaseSpecification<Client>
    {
        public ClientExtendedSpecification(int clientId) : base(c => c.Id == clientId)
        {
            AddInclude(c => c.State);
        }

        public ClientExtendedSpecification() : base(c => true)
        {
            AddInclude(c => c.State);
            AddInclude(c => c.Contacts);
            AddInclude(c => c.Addresses);
        }
    }
}