using Ardalis.GuardClauses;
using Monobits.SharedKernel.Interfaces;
using Monobits.SharedKernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Entities
{
    public class Comment : BaseEntity, IAggregateRoot
    {
        public int Id { get; set; } // <- EF asume que esta es la clave primaria
        public string Comments { get; private set; }
        public int ClientId { get; private set; }
        public Client Client { get; }



        public Comment() { }

        public Comment(string comment, int clientId)
        {
            Guard.Against.NullOrEmpty(comment, nameof(Comments));
            Guard.Against.NegativeOrZero(clientId, nameof(ClientId));

            Comments = comment;
            ClientId = clientId;
        }

        public void Edit(string comment, int clientId)
        {
            Guard.Against.NullOrEmpty(comment, nameof(Comments));
            Guard.Against.NegativeOrZero(clientId, nameof(ClientId));

            Comments = comment;
            ClientId = clientId;
        }
    }
}
