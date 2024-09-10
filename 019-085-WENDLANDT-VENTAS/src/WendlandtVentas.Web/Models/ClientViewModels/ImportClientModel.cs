using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class ImportClientModel
    {
        [Display(Name = "Archivo Excel")]
        public IFormFile ExcelFile { get; set; }
    }
}
