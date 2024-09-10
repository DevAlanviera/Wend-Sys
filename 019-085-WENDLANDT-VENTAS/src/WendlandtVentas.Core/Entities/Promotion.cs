using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Core.Entities
{
    public class Promotion : BaseEntity, IAggregateRoot
    {
        private Classification? _classification;
        private ICollection<ClientPromotion> _clientPromotions;

        public string Name { get; private set; }
        public int Buy { get; private set; }
        public int Present { get; private set; }
        public double Discount { get; private set; }
        public bool IsActive { get; private set; }
        public string DiscountFormat => $"{Math.Round(Discount * 100, 2)}%";
        public Classification? Classification
        {
            get { return _classification; }
            private set
            {
                if (Type == PromotionType.Classification && value.HasValue)
                    _classification = value;
                else
                    _classification = null;
            }
        }
        public ICollection<PresentationPromotion> PresentationPromotions { get; private set; } = new List<PresentationPromotion>();
        public ICollection<ClientPromotion> ClientPromotions
        {
            get
            {
                return _clientPromotions != null ? _clientPromotions : new List<ClientPromotion>();
            }
            private set
            {
                if (Type == PromotionType.Clients)
                    _clientPromotions = ClientPromotions.Union(value).ToList();
                else
                    _clientPromotions = new List<ClientPromotion>();
            }
        }
        public PromotionType Type { get; private set; }
        public Promotion()
        {
        }

        public Promotion(string name, int buy, int present, PromotionType promotionType, Classification? classification, IEnumerable<PresentationPromotion> presentationPromotions, IEnumerable<ClientPromotion> clientPromotions)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.NegativeOrZero(buy, nameof(buy));
            Guard.Against.NegativeOrZero(present, nameof(present));
            Guard.Against.Negative((int)promotionType, nameof(promotionType));
            Guard.Against.Null(presentationPromotions, nameof(presentationPromotions));

            Name = name;
            Buy = buy;
            Present = present;
            Discount = CalculateDiscount();
            Type = promotionType;
            Classification = classification;
            PresentationPromotions = presentationPromotions.ToList();
            ClientPromotions = clientPromotions.ToList();
            IsActive = true;
        }

        public void Edit(string name, int buy, int present, PromotionType promotionType, Classification? classification, IEnumerable<PresentationPromotion> presentationPromotions, IEnumerable<ClientPromotion> clientPromotions)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            Guard.Against.NegativeOrZero(buy, nameof(buy));
            Guard.Against.NegativeOrZero(present, nameof(present));
            Guard.Against.Negative((int)promotionType, nameof(promotionType));
            Guard.Against.Null(presentationPromotions, nameof(presentationPromotions));

            Name = name;
            Buy = buy;
            Present = present;
            Discount = CalculateDiscount();
            Type = promotionType;
            Classification = classification;
            PresentationPromotions = PresentationPromotions.Union(presentationPromotions).ToList();
            ClientPromotions = clientPromotions.ToList();
        }

        public void Delete()
        {
            PresentationPromotions.ToList().ForEach(c => c.Delete());
            ClientPromotions.ToList().ForEach(c => c.Delete());
            Delete(true);
        }
        public void ChangeStatus()
        {
            IsActive = !IsActive;
        }
        private double CalculateDiscount() => (double)Present / ((double)Present + (double)Buy);
    }
}