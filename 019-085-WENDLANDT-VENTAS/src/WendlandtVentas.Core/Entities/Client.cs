using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Core.Entities
{
    public class Client : BaseEntity, IAggregateRoot
    {
        public string Name { get; private set; }
        public Channel? Channel { get; set; }
        public Classification? Classification { get; set; }
        public int? StateId { get; set; }
        public State State { get; set; }
        public string RFC { get; set; }
        [Obsolete]
        public string Address { get; set; }
        public string City { get; set; }
        public string SellerId { get; set; }
        public PayType? PayType { get; set; }
        public int CreditDays { get; set; }
        public ICollection<ClientPromotion> ClientPromotions { get; private set; }
        public ICollection<Address> Addresses { get; private set; }
        public ICollection<Contact> Contacts { get; private set; }

        public Client()
        {
            ClientPromotions = new List<ClientPromotion>();
        }

        public Client(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            Name = name;
        }

        public void Edit(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            Name = name;
        }
    }
}