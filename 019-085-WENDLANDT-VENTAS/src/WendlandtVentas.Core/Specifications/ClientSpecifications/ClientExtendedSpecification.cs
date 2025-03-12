using System;
using System.Linq.Expressions;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ClientSpecifications
{
    public class ClientExtendedSpecification : BaseSpecification<Client>
    {
        // Constructor para obtener un cliente por su Id
        public ClientExtendedSpecification(int clientId) : base(c => c.Id == clientId)
        {
            AddInclude(c => c.State);
        }

        // Constructor para obtener todos los clientes
        public ClientExtendedSpecification() : base(c => true)
        {
            AddInclude(c => c.State);
            AddInclude(c => c.Contacts);
            AddInclude(c => c.Addresses);
        }

        // Nuevo constructor para filtrar clientes con un criterio personalizado
        public ClientExtendedSpecification(Expression<Func<Client, bool>> criteria) : base(criteria)
        {
            AddInclude(c => c.State);
            AddInclude(c => c.Contacts);
            AddInclude(c => c.Addresses);
        }
    }
}