﻿using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Models.ClientViewModels
{
    public class ClientItemModel
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [Display(Name = "Clasificación")]
        public string Classification { get; set; }

        [Display(Name = "Tipo de pago")]
        public string PayType { get; set; }

        [Display(Name = "Canal")]
        public string Channel { get; set; }
        
        [Display(Name = "Estado")]
        public string State { get; set; }
        public string City { get; set; }
        public string RFC { get; set; }
    }
}