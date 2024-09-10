using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Tests
{
    public class ProductBuilder
    {
        private readonly Product _productInstance = new Product();
        
        public ProductBuilder Id(int id)
        {
            _productInstance.Id = id;
            return this;
        }
        
        public Product Build() => _productInstance;
    }
}