using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace WendlandtVentas.Core.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base()
        {
        }

        public ApplicationRole(string roleName) : base(roleName)
        {
        }

        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}