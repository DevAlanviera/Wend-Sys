using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class ContactTableModel
    {
        public int Id { get; set; }
        public string Name { get;  set; }
        public string Cellphone { get;  set; }
        public string OfficePhone { get;  set; }
        public string Email { get;  set; }
        public string Comments { get;  set; }

    }
}
