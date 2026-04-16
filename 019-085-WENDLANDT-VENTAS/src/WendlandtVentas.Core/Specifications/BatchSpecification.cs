using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Ardalis.Specification;
using WendlandtVentas.Core.Entities;



namespace WendlandtVentas.Core.Specifications
{
    public class BatchSpecification : BaseSpecification<Batch>
    {
        // Para Salidas (Out): Solo lotes activos y con stock
        public BatchSpecification(int presentationId)
            : base(b => b.ProductPresentationId == presentationId && b.IsActive && !b.IsDeleted && b.CurrentQuantity > 0)
        {
            ApplyOrderBy(b => b.ExpiryDate); // FIFO
        }

        // Para Ajustes: Lotes activos aunque tengan cantidad 0
        public BatchSpecification(int presentationId, bool includeEmpty)
            : base(b => b.ProductPresentationId == presentationId && b.IsActive && !b.IsDeleted)
        {
            ApplyOrderBy(b => b.BatchNumber);
        }

        // Para buscar un lote específico por su número (Usado en el método In)
        public BatchSpecification(int presentationId, string batchNumber)
            : base(b => b.ProductPresentationId == presentationId &&
                        b.BatchNumber == batchNumber &&
                        !b.IsDeleted)
        {
            // No filtramos por IsActive aquí por si queremos reactivar un lote viejo
        }
    }
}
