using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum Operation
    {
        [Display(Name = "Entrada")] In,
        [Display(Name = "Salida")] Out,
        [Display(Name = "Ajuste")] Adjustment
    }
}