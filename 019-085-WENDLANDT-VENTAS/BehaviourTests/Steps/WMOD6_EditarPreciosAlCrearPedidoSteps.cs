using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace BehaviourTests.Steps
{
    [Binding]
    public class WMOD6_EditarPreciosAlCrearPedidoSteps
    {
        public OrderViewModel Model { get; set; } = new OrderViewModel();

        public WMOD6_EditarPreciosAlCrearPedidoSteps()
        {
        }

        [Given(@"a sale concept which we need a special price")]
        public void GivenASaleConceptWhichWeNeedASpecialPrice()
        {
            Model = new OrderViewModel
            {
                InvoiceCode = "Test",
                IsInvoice = OrderType.Invoice,
                Paid = false,
                PaymentDate = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                PaymentPromiseDate = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                DeliveryDay = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                ClientId = 1,
                Comment = string.Empty,
                Delivery = string.Empty,
                DeliverySpecification = string.Empty,
                ProductPresentationIds = new List<int> { 1, 2, 3 },
                ProductPrices = new List<decimal> { 1, 2, 3},
                Address = string.Empty,
                AddressName = string.Empty,
                PayType = PayType.Cash,
                CurrencyType = CurrencyType.MXN
            };
        }
        
        [When(@"item price is updated")]
        public void WhenItemPriceIsUpdated()
        {
            Model.ProductPrices = new List<decimal> { 10M, 20M, 30M };
        }
        
        [Then(@"sale Total equals the sum of the sale items price")]
        public void ThenSaleTotalEqualsTheSumOfTheSaleItemsPrice()
        {
            Model.ProductPrices.Sum().Should().Be(60);
        }
    }
}