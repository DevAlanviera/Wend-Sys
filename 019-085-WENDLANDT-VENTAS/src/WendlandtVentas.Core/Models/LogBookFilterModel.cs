namespace WendlandtVentas.Core.Models
{
    public class LogBookFilterModel : LogBookModel
    {
        public int Id { get; set; }
        public string ActionType { get; set; }
        public string User { get; set; }
        public string ClientId { get; set; }
        public string Date { get; set; }
        public string RegisterDate { get; set; }
        public string Color { get; set; } 
        public int Take { get; set; }
        public int Skip { get; set; }
    }
}
