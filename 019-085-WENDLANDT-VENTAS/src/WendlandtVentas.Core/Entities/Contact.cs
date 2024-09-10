using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Entities 
{
  public class Contact : BaseEntity, IAggregateRoot
    {
        public string Name { get;private set; }
        public string Cellphone { get; private set; }
        public string OfficePhone { get; private set; }
        public string Email { get; private set; }
        public string Comments { get; private set; }
        public int ClientId { get; private set; }
        public Client Client { get;}

        public Contact() { }

        public Contact(string name, string cellPhone, string officePhone, string email, string comments, int clientId)
        {
            Guard.Against.NullOrEmpty(name, nameof(Name));
            //Guard.Against.NullOrEmpty(email, nameof(email));
            Guard.Against.NegativeOrZero(clientId, nameof(clientId)); 
            Name = name;
            Cellphone = cellPhone;
            OfficePhone = officePhone;
            Email = email;
            Comments = comments;
            ClientId = clientId;
        }

        public void Edit(string name, string cellPhone, string officePhone, string email, string comments, int clientId)
        {
            Guard.Against.NullOrEmpty(name, nameof(Name));
            //Guard.Against.NullOrEmpty(email, nameof(email));
            Guard.Against.NegativeOrZero(clientId, nameof(clientId));

            Name = name;
            Cellphone = cellPhone;
            OfficePhone = officePhone;
            Email = email;
            Comments = comments;
            ClientId = clientId;
        }
    }
}
