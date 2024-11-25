using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;

namespace WendlandtVentas.Core.Entities
{
    public class Presentation : BaseEntity, IAggregateRoot
    {

        //gregamos el Id para poder relacionarnos con los litros del producto por medio de este dato.
        public int Id { get; set; }
        public string Name { get; private set; }
        public double Liters { get; private set; }
        public ICollection<ProductPresentation> ProductPresentations { get; private set; }
        public ICollection<PresentationPromotion> PresentationPromotions { get; private set; }
        public Presentation()
        {
            ProductPresentations = new List<ProductPresentation>();
        }

        public Presentation(string name, double liters)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.Zero(liters, nameof(liters));

            Name = name;
            Liters = liters;
        }

        public string NameExtended() => $"{Name} {Liters} lts.";
    }
}