using Ardalis.GuardClauses;
using DocumentFormat.OpenXml.Drawing.Charts;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

//WendlandtVentas.Core/Entities/Bitacora.cs

namespace WendlandtVentas.Core.Entities
{
    public class Bitacora
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Registro_id { get; set; }
        public string Usuario { get; set; }
        public DateTime Fecha_modificacion { get; set; }

        public string Accion { get; set; }

        public Bitacora(int registro_id, string usuario, string accion)
        {
            this.Registro_id = registro_id;
            this.Usuario = usuario;
            Fecha_modificacion = DateTime.UtcNow; // O DateTime.Now si prefieres la hora local
            this.Accion = accion;
        }
    }
}