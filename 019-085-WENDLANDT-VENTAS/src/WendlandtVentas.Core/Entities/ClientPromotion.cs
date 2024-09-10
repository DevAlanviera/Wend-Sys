using Ardalis.GuardClauses;
using Monobits.SharedKernel.Interfaces;
using System;

namespace WendlandtVentas.Core.Entities
{
    public class ClientPromotion : IAggregateRoot
    {
        public int ClientId { get; private set; }
        public Client Client { get; private set; }
        public int PromotionId { get; private set; }
        public Promotion Promotion { get; private set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; private set; }
        public ClientPromotion() { }
        public ClientPromotion(int clientId)
        {
            Guard.Against.NegativeOrZero(clientId, nameof(clientId));

            ClientId = clientId;
        }
        public void Delete(bool isDeleted = true)
        {
            IsDeleted = isDeleted;
        }
    }
}