using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class UpdateAmountDto
    {
        public int OrderId { get; set; }
        public decimal RealAmount { get; set; }
        public string TransferToken { get; set; }
    }
}
