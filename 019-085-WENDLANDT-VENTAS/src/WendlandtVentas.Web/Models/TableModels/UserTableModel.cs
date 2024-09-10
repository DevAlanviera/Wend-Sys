namespace WendlandtVentas.Web.Models.TableModels
{
    public class UserTableModel
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Roles { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public bool CanDisableOrDelete { get; set; }
        public string CreatedAt { get; set; }
    }
}
