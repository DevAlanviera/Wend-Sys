using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class OrderIdRequestDto
    {
        public int OrderId { get; set; }
        public string TransferToken { get; set; }
    }
}
