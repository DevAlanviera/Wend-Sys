using Ardalis.GuardClauses;
using WendlandtVentas.Core.Entities.Enums;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;

namespace WendlandtVentas.Core.Entities
{
    public class Movement : BaseEntity, IAggregateRoot
    {
        public int ProductPresentationId { get; private set; }
        public ProductPresentation ProductPresentation { get; private set; }

        // --- Nueva relación con Batch ---
        public int? BatchId { get; set; } // Opcional para movimientos antiguos o sin lote
        public virtual Batch Batch { get; set; }
        // -

        public string Comment { get; private set; }
        public string UserId { get; private set; }
        public int Quantity { get; private set; }
        public int QuantityOld { get; private set; }

        public int QuantityCurrent { get; private set; }

        private Operation operation;
        public Operation Operation
        {
            get
            {
                return operation;
            }
            set
            {
                operation = value;

                switch (Operation)
                {
                    case Operation.In:
                        QuantityCurrent = QuantityOld + Quantity;
                        break;
                    case Operation.Out:
                        QuantityCurrent = QuantityOld - Quantity;
                        break;
                    case Operation.Adjustment:
                        QuantityCurrent = Quantity;
                        break;
                }
            }
        }
        public Movement()
        {
        }

        // Constructor actualizado para incluir BatchId
        public Movement(int productPresentationId, int quantity, Operation operation, int quantityOld, string comment, string userId, int? batchId = null)
        {
            Guard.Against.OutOfRange(productPresentationId, nameof(productPresentationId), 1, int.MaxValue);
            Guard.Against.OutOfRange(quantity, nameof(quantity), 1, int.MaxValue);
            Guard.Against.Null(operation, nameof(operation));
            Guard.Against.OutOfRange(quantityOld, nameof(quantityOld), int.MinValue, int.MaxValue);
            Guard.Against.NullOrEmpty(userId, nameof(userId));

            ProductPresentationId = productPresentationId;
            Quantity = quantity;
            QuantityOld = quantityOld;
            Operation = operation;
            Comment = comment;
            UserId = userId;
            BatchId = batchId; // Asignación del lote
        }
    }
}