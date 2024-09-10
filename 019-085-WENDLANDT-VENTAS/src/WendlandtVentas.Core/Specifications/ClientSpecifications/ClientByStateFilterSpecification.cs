using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;
namespace WendlandtVentas.Core.Specifications.ClientSpecifications
{
    public class ClientByStateFilterSpecification : BaseSpecification<Client>
    {
        public ClientByStateFilterSpecification(string[] stateId) : base(c => true)
        {
            if(stateId.Any())
                AppendCriteria(c => c.StateId.HasValue && stateId.Contains(c.StateId.Value.ToString()), true);
        }
    }
}