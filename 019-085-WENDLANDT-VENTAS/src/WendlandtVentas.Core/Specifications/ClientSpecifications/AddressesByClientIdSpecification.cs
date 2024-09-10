using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;
namespace WendlandtVentas.Core.Specifications.ClientSpecifications
{
    public class AddressesByClientIdSpecification : BaseSpecification<Address>
    {
        public AddressesByClientIdSpecification(int id) : base(c => c.ClientId == id && !c.IsDeleted)
        {
           
        }
    }
}