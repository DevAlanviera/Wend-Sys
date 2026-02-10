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
        // El error CS7036 se quita pasando el filtro al constructor base
        public BatchSpecification(int productPresentationId, string batchNumber)
            : base(b => b.ProductPresentationId == productPresentationId &&
                        b.BatchNumber == batchNumber &&
                        !b.IsDeleted)
        {
            // Aquí puedes agregar Includes si tu Kernel lo permite, por ejemplo:
            // AddInclude(b => b.ProductPresentation);
        }

        // Sobrecarga para obtener todos los lotes de una presentación
        public BatchSpecification(int productPresentationId)
            : base(b => b.ProductPresentationId == productPresentationId &&
                        !b.IsDeleted &&
                        b.CurrentQuantity > 0)
        {
        }
    }
}
