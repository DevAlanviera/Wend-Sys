using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Entities
{
    public class ProductBundleComponent
    {
        public int Id { get; set; }
        public int BundleProductId { get; set; } // El ID del "12-pack Costco"
        public virtual Product BundleProduct { get; set; }

        public int ComponentProductId { get; set; } // El ID de la "Foca Parlante Botella"
        public virtual Product ComponentProduct { get; set; }

        public int Quantity { get; set; } // Para un 12-pack, aquí pondrías "12" (o la suma de varios)
    }
}
