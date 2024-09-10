namespace WendlandtVentas.Core
{
    public class EmailSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public string From { get; set; }
    }
}