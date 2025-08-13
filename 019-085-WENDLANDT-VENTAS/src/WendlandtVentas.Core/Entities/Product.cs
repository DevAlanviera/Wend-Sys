using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Web.Models.ProductViewModels;

namespace WendlandtVentas.Core.Entities
{
    public class Product : BaseEntity, IAggregateRoot
    {
        public string Name { get; private set; }
        public Distinction Distinction { get; private set; }
        public string Season { get; private set; }
        public ICollection<ProductPresentation> ProductPresentations { get; private set; }

        // Propiedad de navegación para PrecioEspecial
        public virtual ICollection<PrecioEspecial> PreciosEspeciales { get; set; }
        public Product()
        {
            ProductPresentations = new List<ProductPresentation>();
        }
        public Product(string name, Distinction distinction, string season)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.Negative((int)distinction, nameof(distinction));

            Name = name;
            Distinction = distinction;
            Season = distinction == Distinction.Season ? season : string.Empty;
        }

        public void Edit(string name, Distinction distinction, string season)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.Negative((int)distinction, nameof(distinction));

            Name = name;
            Distinction = distinction;
            Season = distinction == Distinction.Season ? season : string.Empty;
        }
    }
}