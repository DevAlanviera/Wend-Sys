using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class ActualizarTotalViewModel
    {
        public int Id { get; set; } // ID de la orden
        public bool PrecioEspecial { get; set; } // Checkbox
        public decimal RealAmount { get; set; } // Nuevo total si aplica
    }
}
