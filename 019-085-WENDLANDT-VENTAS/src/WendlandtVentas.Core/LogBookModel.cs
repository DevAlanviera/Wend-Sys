using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core
{
    public class LogBookModel
    {
        public int Id { get; set; }
        public string ActionType { get; set; }
        public string Target { get; set; }
        public string User { get; set; }
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public string Color { get; set; }
        public string RegisterDate { get; set; }
        public DateTime? Date { get; set; }
        public string Json { get; set; }
    }
}
