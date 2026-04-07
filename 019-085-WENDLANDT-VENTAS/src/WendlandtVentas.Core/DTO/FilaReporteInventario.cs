using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.DTO
{
    public class FilaReporteInventario
    {
        public string Cerveza { get; set; }

        public bool EsTemporada { get; set; }
        public int CajaBotella { get; set; }
        public int BotellaSuelta { get; set; }
        public int Sesentas { get; set; }
        public int VeintesSteel { get; set; }
        public int VeintesPet { get; set; }
        public int CajaLata { get; set; }
        public int LataSuelta { get; set; }
        public string Lote { get; set; }
        public string Caducidad { get; set; }
    }
}
