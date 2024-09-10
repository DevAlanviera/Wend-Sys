using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;

namespace WendlandtVentas.Core.Entities
{
    public class State : BaseEntity, IAggregateRoot
    {
        public string Name { get; private set; }
        public ICollection<Client> Clients { get; set; }

        public State()
        {
        }

        public State(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            Name = name;
        }
    }
}
