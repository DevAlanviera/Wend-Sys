using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Core.Entities
{
    public class Order : BaseEntity, IAggregateRoot
    {
        public string RemissionCode { get; private set; }
        public string InvoiceCode { get; private set; }
        public bool Paid { get; private set; }
        public OrderType Type { get; private set; }
        public OrderStatus OrderStatus { get; private set; }
        public DateTime PaymentPromiseDate { get; private set; }
        public DateTime PaymentDate { get; private set; }
        public DateTime DeliveryDate { get; private set; }
        public DateTime DueDate { get; private set; }
        public decimal Discount { get; private set; }
        public decimal SubTotal { get; private set; }
        public decimal Total { get; private set; }
        public string UserId { get; private set; }
        public string Comment { get; private set; }
        public string CollectionComment { get; private set; }
        public int ClientId { get; private set; }
        public Client Client { get; private set; }
        public string AddressName { get; private set; }
        public string Address { get; private set; }
        public PayType? PayType { get; private set; }
        public ICollection<OrderProduct> OrderProducts { get; private set; } = new List<OrderProduct>();
        public ICollection<OrderPromotion> OrderPromotions { get; private set; } = new List<OrderPromotion>();
        public string Delivery { get; private set; }
        public string DeliverySpecification { get; private set; }
        public bool InventoryDiscount { get; private set; }
        public CurrencyType CurrencyType { get; private set; }

        public decimal BaseAmount
        {
            //Fórmula anterior (SubTotal / 1.265M) * 0.8M;
            get
            {
                if (Type == OrderType.Invoice)
                    if (SubTotal > 0)
                        return (SubTotal / 1.265M);

                return 0;
            }
        }

        //  public decimal Distribution => BaseAmount * 0.3163M;
        public decimal IEPS => BaseAmount * 0.265M;
        public decimal IVA => (BaseAmount + IEPS) * 0.16M; //Fórmula anterior (BaseAmount + Distribution + IEPS) * 0.16M


        public Order() { }

        public Order(string invoiceCode, OrderType type, OrderStatus orderStatus,
            bool paid, DateTime paymentPromiseDate, DateTime paymentDate, string userId, int clientId,
            string comment, string delivery, string deliverySpecification, IEnumerable<OrderProduct> orderProducts,
            IEnumerable<OrderPromotion> orderPromotions, string address, string addressName, DateTime deliveryDate,
            DateTime dueDate, PayType payType, CurrencyType currencyType)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.Null(orderStatus, nameof(orderStatus));
            Guard.Against.Null(paid, nameof(paid));
            Guard.Against.Null(paymentPromiseDate, nameof(paymentPromiseDate));
            Guard.Against.Null(paymentDate, nameof(paymentDate));
            Guard.Against.NullOrEmpty(userId, nameof(userId));
            Guard.Against.NegativeOrZero(clientId, nameof(clientId));
            Guard.Against.NullOrEmpty(orderProducts, nameof(orderProducts));

            //RemissionCode = remissionCode;
            InvoiceCode = invoiceCode;
            OrderStatus = orderStatus;
            Type = type;
            Paid = paid;
            PaymentPromiseDate = paymentPromiseDate;
            PaymentDate = paymentDate;
            DeliveryDate = deliveryDate;
            DueDate = dueDate;
            UserId = userId;
            ClientId = clientId;
            Address = address;
            AddressName = addressName;
            Comment = comment;
            OrderProducts = orderProducts.ToList();
            SubTotal = orderProducts.Where(p => !p.IsPresent).Sum(c => c.Price * c.Quantity);
            Delivery = delivery;
            DeliverySpecification = deliverySpecification;
            OrderPromotions = orderPromotions.ToList();
            Discount = orderPromotions.Sum(c => c.Discount);
            Total = (Type == OrderType.Remission ? SubTotal : BaseAmount + IEPS + IVA) - Discount; //Fórmula anterior (Type == OrderType.Remission ? SubTotal : BaseAmount + Distribution + IEPS + IVA) - Discount
            PayType = payType;
            CurrencyType = currencyType;
        }

        public void Edit(string invoiceCode, OrderType type, OrderStatus orderStatus,
            bool paid, DateTime paymentPromiseDate, DateTime paymentDate, int clientId, string comment,
            string delivery, string deliverySpecification, IEnumerable<OrderProduct> orderProducts,
            IEnumerable<OrderPromotion> orderPromotions, string address, string addressName, DateTime deliveryDate,
            DateTime dueDate, PayType payType, CurrencyType currencyType)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.Null(orderStatus, nameof(orderStatus));
            Guard.Against.Null(paid, nameof(paid));
            Guard.Against.Null(paymentPromiseDate, nameof(paymentPromiseDate));
            Guard.Against.Null(paymentDate, nameof(paymentDate));
            Guard.Against.NegativeOrZero(clientId, nameof(clientId));
            Guard.Against.NullOrEmpty(orderProducts, nameof(orderProducts));

            //RemissionCode = remissionCode;
            InvoiceCode = invoiceCode;
            OrderStatus = OrderStatus;
            Paid = paid;
            PaymentPromiseDate = paymentPromiseDate;
            PaymentDate = paymentDate;
            DeliveryDate = deliveryDate;
            DueDate = dueDate;
            ClientId = clientId;
            Address = address;
            AddressName = addressName;
            Comment = comment;
            OrderProducts = new List<OrderProduct>() { };
            OrderProducts = orderProducts.ToList();
            SubTotal = orderProducts.Where(p => !p.IsPresent).Sum(c => c.Price * c.Quantity);
            Type = type;
            Delivery = delivery;
            DeliverySpecification = deliverySpecification;
            OrderPromotions = orderPromotions.ToList();
            Discount = orderPromotions.Sum(c => c.Discount);
            Total = (Type == OrderType.Remission ? SubTotal : BaseAmount + IEPS + IVA) - Discount;
            PayType = payType;
            CurrencyType = currencyType;
        }

        public void ChangeStatus(OrderStatus orderStatus, string comment, string invoiceCode)
        {
            Guard.Against.Null(orderStatus, nameof(orderStatus));
            OrderStatus = orderStatus;
            Comment = comment;
            InvoiceCode = invoiceCode;
        }

        public void Delivered(DateTime deliveryDate, DateTime dueDate)
        {
            DeliveryDate = deliveryDate;
            DueDate = dueDate;
        }

        public void GenerateRemisionCode()
        {
            var digits = Id.ToString();

            var idLenght = digits.Length;

            if (idLenght < 6)
            {
                for (var counter = idLenght; counter < 6; counter++)
                {
                    digits = digits.Insert(0, "0");
                }
            }

            RemissionCode = $"{DateTime.Now.Year}{digits}";
        }

        public void ToggleInventoryDiscount()
        {
            InventoryDiscount = !InventoryDiscount;
        }

        public void AddCollectionComment(string collectionComment)
        {
            CollectionComment = collectionComment;
        }

        public void UpdateReturnInformation(string invoiceReturnNumber, string returnReason)
        {
            RemissionCode = invoiceReturnNumber;
            CollectionComment = returnReason;
        }
    }
}