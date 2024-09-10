using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;
namespace WendlandtVentas.Core.Specifications.ClientSpecifications
{
    public class StateByNameSpecification : BaseSpecification<State>
    {
        public StateByNameSpecification(string name) : base(c => c.Name.ToLower().Trim().Equals(name))
        {
        
        }
    }
}