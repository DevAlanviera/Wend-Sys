using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace WendlandtVentas.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}