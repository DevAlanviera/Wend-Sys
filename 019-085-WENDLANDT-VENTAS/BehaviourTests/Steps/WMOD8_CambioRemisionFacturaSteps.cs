using FluentAssertions;
using Monobits.SharedKernel.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using TechTalk.SpecFlow;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace BehaviourTests.Steps
{
    [Binding]
    public class WMOD8_CambioRemisionFacturaSteps
    {
        private Mock<IRepository> _repository { get; set; }
        private Order _order { get; set; } = new Order();
        private List<OrderProduct> _orderProducts { get; set; } = new List<OrderProduct>();
        public OrderViewModel Model { get; set; } = new OrderViewModel();
        private (DateTime PaymentDate, DateTime PaymentPromiseDate, DateTime DeliveryDay) _dates { get; set; }
        public decimal _orderTotal { get; set; }

        public WMOD8_CambioRemisionFacturaSteps()
        {
            _repository = new Mock<IRepository>();

            _orderProducts = new List<OrderProduct>
            {
                new OrderProduct(new ProductPresentation(1, 1, 10, 10, 10), 1 , false, 10)
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
                ProductPresentationIds = new List<int> { 1, 2, 3},
                Address = string.Empty,
                AddressName = string.Empty,
                PayType = PayType.Cash,
                CurrencyType = CurrencyType.MXN
            };

            _dates = GetParsePaymentDate(Model.PaymentDate, Model.PaymentPromiseDate, Model.DeliveryDay);
        }

        [Given(@"a remision sale type")]
        public void GivenARemisionSaleType()
        {
            Model.IsInvoice = OrderType.Remission;

            _order = new Order(Model.InvoiceCode, Model.IsInvoice, OrderStatus.New, Model.Paid, _dates.PaymentPromiseDate,
                    _dates.PaymentDate, "1", Model.ClientId, Model.Comment, Model.Delivery, Model.DeliverySpecification,
                    _orderProducts, new List<OrderPromotion>(), Model.Address, Model.AddressName, _dates.DeliveryDay,
                    _dates.DeliveryDay.AddDays(1), Model.PayType, Model.CurrencyType);

            _repository.Setup(c => c.Add(_order)).Returns(_order);
            _order = _repository.Object.Add(_order);

            _orderTotal = _order.Total;
        }

        [Given(@"a facturacion sale type")]
        public void GivenAFacturacionSaleType()
        {
            Model.IsInvoice = OrderType.Invoice;

            _order = new Order(Model.InvoiceCode, Model.IsInvoice, OrderStatus.New, Model.Paid, _dates.PaymentPromiseDate,
                    _dates.PaymentDate, "1", Model.ClientId, Model.Comment, Model.Delivery, Model.DeliverySpecification,
                    _orderProducts, new List<OrderPromotion>(), Model.Address, Model.AddressName, _dates.DeliveryDay,
                    _dates.DeliveryDay.AddDays(0), Model.PayType, Model.CurrencyType);

            _repository.Setup(c => c.Add(_order)).Returns(_order);
            _order = _repository.Object.Add(_order);

            _orderTotal = _order.Total;
        }

        [When(@"its type is changed to facturacion")]
        public void WhenItsTypeIsChangedToFacturacion()
        {
            Model.IsInvoice = OrderType.Invoice;

            _order.Edit(Model.InvoiceCode, Model.IsInvoice, OrderStatus.New, Model.Paid,
                _dates.PaymentPromiseDate, _dates.PaymentDate, Model.ClientId, Model.Comment, Model.Delivery,
                Model.DeliverySpecification, _orderProducts, new List<OrderPromotion>(), Model.Address, Model.AddressName,
                _dates.DeliveryDay, _dates.DeliveryDay.AddDays(1), Model.PayType, Model.CurrencyType);

            _repository.Setup(c => c.Update(_order));
            _repository.Object.Update(_order);
        }

        [When(@"its type is changed to remision")]
        public void WhenItsTypeIsChangedToRemision()
        {
            Model.IsInvoice = OrderType.Remission;

            _order.Edit(Model.InvoiceCode, Model.IsInvoice, OrderStatus.New, Model.Paid,
                _dates.PaymentPromiseDate, _dates.PaymentDate, Model.ClientId, Model.Comment, Model.Delivery,
                Model.DeliverySpecification, _orderProducts, new List<OrderPromotion>(), Model.Address, Model.AddressName,
                _dates.DeliveryDay, _dates.DeliveryDay.AddDays(1), Model.PayType, Model.CurrencyType);

            _repository.Setup(c => c.Update(_order));
            _repository.Object.Update(_order);
        }

        [Then(@"the sale total is recalculated with taxes")]
        public void ThenTheSaleTotalIsRecalculatedWithTaxes()
        {
            _order.Total.Should().BeGreaterThan(_orderTotal);
        }

        [Then(@"the sale total is recalculated with NO taxes")]
        public void ThenTheSaleTotalIsRecalculatedWithNOTaxes()
        {
            _order.Total.Should().BeLessThan(_orderTotal);
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