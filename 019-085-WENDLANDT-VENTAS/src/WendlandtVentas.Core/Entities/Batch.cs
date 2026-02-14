using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Entities
{
    public class Batch : BaseEntity, IAggregateRoot
    {
        public string BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int ProductPresentationId { get; set; }

        // Cantidades para control de saldo por lote
        public int InitialQuantity { get; set; }
        public int CurrentQuantity { get; set; }
        public bool IsActive { get; set; } = true;

        // Propiedad de navegación
        public virtual ProductPresentation ProductPresentation { get; set; }
        public virtual ICollection<Movement> Movements { get; set; }



        public Batch()
        {
            Movements = new HashSet<Movement>();
            IsActive = true;
        }

        public Batch(string batchNumber, DateTime expiryDate, int productPresentationId, int quantity) : this()
        {
            BatchNumber = batchNumber;
            ExpiryDate = expiryDate;
            ProductPresentationId = productPresentationId;
            InitialQuantity = quantity;
            CurrentQuantity = quantity;
        }
    }
}
