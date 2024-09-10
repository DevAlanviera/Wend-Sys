using FluentAssertions;
using Monobits.SharedKernel.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TechTalk.SpecFlow;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace BehaviourTests.Steps
{
    [Binding]
    public class WMOD17_CalcularPesoDelPedidoSteps
    {
        private Mock<IRepository> _repository { get; set; }
        private Order _order { get; set; } = new Order();
        private List<OrderProduct> _orderProducts { get; set; } = new List<OrderProduct>();
        public OrderViewModel Model { get; set; } = new OrderViewModel();
        private (DateTime PaymentDate, DateTime PaymentPromiseDate, DateTime DeliveryDay) _dates { get; set; }
        public decimal _orderWeight { get; set; }

        public WMOD17_CalcularPesoDelPedidoSteps()
        {
            _repository = new Mock<IRepository>();

        }

        [Given(@"a sale concept which we want to know the sale weight")]
        public void GivenASaleConceptWhichWeWantToKnowTheSaleWeight()
        {
            _orderProducts = new List<OrderProduct>
            {
                new OrderProduct(new ProductPresentation(1, 1, 10, 10, 10), 1 , false, 10),
                new OrderProduct(new ProductPresentation(1, 1, 10, 10, 20), 2 , false, 10)
            };

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
                Address = string.Empty,
                AddressName = string.Empty,
                PayType = PayType.Cash,
                CurrencyType = CurrencyType.MXN
            };

            _dates = GetParsePaymentDate(Model.PaymentDate, Model.PaymentPromiseDate, Model.DeliveryDay);
        }
        
        [When(@"sale item list is updated")]
        public void WhenSaleItemListIsUpdated()
        {
            _order = new Order(Model.InvoiceCode, Model.IsInvoice, OrderStatus.New, Model.Paid, _dates.PaymentPromiseDate,
                    _dates.PaymentDate, "1", Model.ClientId, Model.Comment, Model.Delivery, Model.DeliverySpecification,
                    _orderProducts, new List<OrderPromotion>(), Model.Address, Model.AddressName, _dates.DeliveryDay,
                    _dates.DeliveryDay.AddDays(1), Model.PayType, Model.CurrencyType);

            _repository.Setup(c => c.Add(_order)).Returns(_order);
            _order = _repository.Object.Add(_order);
        }
        
        [Then(@"sale weight equals the sum of the current items weight")]
        public void ThenSaleWeightEqualsTheSumOfTheCurrentItemsWeight()
        {
            _orderWeight = _order.OrderProducts.Sum(c => c.ProductPresentation.Weight * c.Quantity);

            _orderWeight.Should().Be(50);
        }

        private (DateTime PaymentDate, DateTime PaymentPromiseDate, DateTime DeliveryDay) GetParsePaymentDate(string paymentDateVal, string paymentPromiseDateVal, string deliveryDayVal)
        {
            var paymentDate = DateTime.MinValue;
            var paymentPromiseDate = DateTime.MinValue;
            var deliveryDay = DateTime.MinValue;

            if (!string.IsNullOrEmpty(paymentDateVal))
                paymentDate = ParseExact(paymentDateVal);

            if (!string.IsNullOrEmpty(paymentPromiseDateVal))
                paymentPromiseDate = ParseExact(paymentPromiseDateVal);

            if (!string.IsNullOrEmpty(deliveryDayVal))
                deliveryDay = ParseExact(deliveryDayVal);

            return (paymentDate, paymentPromiseDate, deliveryDay);
        }

        private DateTime ParseExact(string date)
        {
            return DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }
}
