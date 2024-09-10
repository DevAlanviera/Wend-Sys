using System.Collections.Generic;

namespace WendlandtVentas.Web.Models
{
    public class SidebarViewModel
    {
        public string CurrentPath { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public string Role { get; set; }
    }
}