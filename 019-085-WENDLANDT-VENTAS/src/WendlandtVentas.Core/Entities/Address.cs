using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;

namespace WendlandtVentas.Core.Entities
{
    public class Address : BaseEntity, IAggregateRoot
    {
        public string Name { get; private set; }
        public string AddressLocation { get; private set; }
        public int ClientId { get; private set; }
        public Client Client { get; }

        public Address() { }

        public Address(string name, string addressLocation, int clientId)
        {
            Guard.Against.NullOrEmpty(name, nameof(Name));
            Guard.Against.NullOrEmpty(addressLocation, nameof(AddressLocation));
            Guard.Against.NegativeOrZero(clientId, nameof(ClientId));

            Name = name;
            AddressLocation = addressLocation;
            ClientId = clientId;
        }

        public void Edit(string name, string addressLocation, int clientId)
        {
            Guard.Against.NullOrEmpty(name, nameof(Name));
            Guard.Against.NullOrEmpty(addressLocation, nameof(AddressLocation));
            Guard.Against.NegativeOrZero(clientId, nameof(clientId));

            Name = name;
            AddressLocation = addressLocation;
            ClientId = clientId;
        }
    }
}