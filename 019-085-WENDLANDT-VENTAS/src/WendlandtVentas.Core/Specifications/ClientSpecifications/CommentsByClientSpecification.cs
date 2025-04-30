using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ClientSpecifications
{
    public class CommentsByClientIdSpecification : BaseSpecification<Comment>
    {
        public CommentsByClientIdSpecification(int clientId)
            : base(c => c.ClientId == clientId && !c.IsDeleted) { }
    }

}
