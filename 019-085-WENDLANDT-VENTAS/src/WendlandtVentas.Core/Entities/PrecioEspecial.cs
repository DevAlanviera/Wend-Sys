
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Web.Models.ProductViewModels;

namespace WendlandtVentas.Web.Models.ProductViewModels
{
    public class PrecioEspecial
    {
        // Clave foránea y parte de la clave compuesta
        [Key, Column(Order = 0)]
        public int ClienteId { get; set; }

        // Clave foránea y parte de la clave compuesta
        [Key, Column(Order = 1)]
        public int ProductoId { get; set; }

        // Precio especial para el cliente y producto
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Precio { get; set; }

        // Relación con la tabla Cliente
        [ForeignKey("ClienteId")]
        public virtual Client Cliente { get; set; }

        // Relación con la tabla Producto
        [ForeignKey("ProductoId")]
        public virtual Product Producto { get; set; }
    }
}