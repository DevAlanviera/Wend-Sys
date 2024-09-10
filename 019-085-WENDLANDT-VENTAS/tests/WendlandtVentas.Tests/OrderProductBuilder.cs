using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Tests
{
    public class OrderProductBuilder
    {
        private readonly OrderProduct _orderProductInstance = new OrderProduct(new ProductPresentation(1, 1, 100, 100, 100), 10, false, 10);

        public OrderProductBuilder Id(int id)
        {
            _orderProductInstance.Id = id;
            return this;
        }

        public OrderProduct Build() => _orderProductInstance;
    }
}