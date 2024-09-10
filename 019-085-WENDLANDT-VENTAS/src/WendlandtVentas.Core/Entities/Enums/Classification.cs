using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum Classification
    {
        [Display(Name = "Oro")] Gold, 
        [Display(Name = "Plata")] Silver,
        [Display(Name = "Bronce")] Bronze

    }
}