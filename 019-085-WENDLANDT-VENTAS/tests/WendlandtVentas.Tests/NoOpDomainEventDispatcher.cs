using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Threading.Tasks;

namespace WendlandtVentas.Tests
{
    public class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public async Task Dispatch(BaseDomainEvent domainEvent) { }
    }
}