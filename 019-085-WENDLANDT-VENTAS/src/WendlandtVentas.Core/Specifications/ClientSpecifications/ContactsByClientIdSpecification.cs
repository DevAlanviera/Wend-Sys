using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ClientSpecifications
{
    public class ContactsByClientIdSpecification : BaseSpecification<Contact>
    {
        public ContactsByClientIdSpecification(int id) : base(c => c.ClientId == id)
        {
           
        }
    }
}