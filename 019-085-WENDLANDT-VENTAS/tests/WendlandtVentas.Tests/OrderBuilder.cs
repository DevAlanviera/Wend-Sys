using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Tests
{
    public class OrderBuilder
    {
        private readonly Order _orderInstance = new Order();
        
        public OrderBuilder Id(int id)
        {
            _orderInstance.Id = id;
            return this;
        }
        
        public Order Build() => _orderInstance;
    }
}