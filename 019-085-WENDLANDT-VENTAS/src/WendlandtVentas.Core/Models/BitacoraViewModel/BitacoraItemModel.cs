using System;

namespace WendlandtVentas.Core.Models.BitacoraViewModel
{
    public class BitacoraItemModel
    {
        public int Id { get; set; }
        public int Registro_Id { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaModificacion { get; set; }

        public string Accion { get; set; }
    }
}