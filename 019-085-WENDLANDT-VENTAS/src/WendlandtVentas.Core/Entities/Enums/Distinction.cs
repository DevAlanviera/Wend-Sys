using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum Distinction
    {
        [Display(Name = "Lupulosas")] Hops,
        [Display(Name = "No lupulosas")] NotHops,
        [Display(Name = "Temporada")] Season,
        [Display(Name = "Wellen")] Wellen
    }
}