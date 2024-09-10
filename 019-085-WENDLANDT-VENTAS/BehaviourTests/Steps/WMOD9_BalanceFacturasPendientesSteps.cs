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
using WendlandtVentas.Web.Models.OrderViewModels;

namespace BehaviourTests.Steps
{
    [Binding]
    public class WMOD9_BalanceFacturasPendientesSteps
    {
        private Mock<IRepository> _repository { get; set; }
        private Order _order { get; set; } = new Order();
        public OrderStatusViewModel Model { get; set; } = new OrderStatusViewModel();
        private (DateTime PaymentDate, DateTime PaymentPromiseDate, DateTime DeliveryDay) _dates { get; set; }
        private OrderStatus _currentStatus { get; set; }
        private DateTime _currentDeliveryDate { get; set; }
        private DateTime _currentDueDate { get; set; }

        public WMOD9_BalanceFacturasPendientesSteps()
        {
            _repository = new Mock<IRepository>();
        }

        [Given(@"a sale status change")]
        public void GivenASaleStatusChange()
        {
            var orderProducts = new List<OrderProduct>
            {
                new OrderProduct(new ProductPresentation(1, 1, 10, 10, 10), 1 , false, 10)
            };

            var model = new OrderViewModel
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

            var dates = GetParsePaymentDate(model.PaymentDate, model.PaymentPromiseDate, model.DeliveryDay);

            _order = new Order(model.InvoiceCode, model.IsInvoice, OrderStatus.New, model.Paid, _dates.PaymentPromiseDate,
                       dates.PaymentDate, "1", model.ClientId, model.Comment, model.Delivery, model.DeliverySpecification,
                       orderProducts, new List<OrderPromotion>(), model.Address, model.AddressName, _dates.DeliveryDay,
                       _dates.DeliveryDay.AddDays(1), model.PayType, model.CurrencyType);

            _repository.Setup(c => c.Add(_order)).Returns(_order);
            _order = _repository.Object.Add(_order);

            _currentStatus = _order.OrderStatus;
            _currentDeliveryDate = _order.DeliveryDate;
            _currentDueDate = _order.DueDate;
        }

        [When(@"the sale status is changed to ""(.*)""")]
        public void WhenTheSaleStatusIsChangedTo(string p0)
        {
            Model.Status = OrderStatus.Delivered;

            _order.ChangeStatus(Model.Status, Model.Comments ?? "", Model.InvoiceCode ?? "");
        }

        [Then(@"sale due date is set to today \+ credit days")]
        public void ThenSaleDueDateIsSetToTodayCreditDays()
        {
            var creditDays = 30;
            _repository.Setup(c => c.Update(_order));
            try
            {
                _order.Delivered(DateTime.UtcNow, DateTime.UtcNow.AddDays(creditDays));

                _repository.Object.Update(_order);
            }
            catch (Exception ex)
            {
                _order.ChangeStatus(_currentStatus, Model.Comments ?? "", Model.InvoiceCode ?? "");
                _order.Delivered(_currentDeliveryDate, _currentDueDate);

                _repository.Object.Update(_order);

                Console.WriteLine(ex.Message);
            }

            _order.DueDate.Date.Should().Be(DateTime.UtcNow.AddDays(creditDays).Date);
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