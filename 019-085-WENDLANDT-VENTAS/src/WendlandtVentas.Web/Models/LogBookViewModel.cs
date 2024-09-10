 using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core;

namespace WendlandtVentas.Web.Models
{
    public class LogBookViewModel : LogBookModel  
    {
        public string User { get; set; } 
        public string Date { get; set; }
        public string RegisterDate { get; set; }
        public string Color { get; set; }
        public List<SelectListItem> Users { get; set; }
        public Dictionary<string, object> Content { get; set; } = new Dictionary<string, object>();
    }
}
