namespace WendlandtVentas.Web.Models.TableModels
{
    public class NotificationTableModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string CreatedAt { get; set; }
        public bool Active { get; set; }
    }
}
