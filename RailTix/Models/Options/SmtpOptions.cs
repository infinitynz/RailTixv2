namespace RailTix.Models.Options
{
    public class SmtpOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 2525;
        public bool UseSsl { get; set; } = false;
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string FromAddress { get; set; } = "no-reply@railtix.local";
        public string FromName { get; set; } = "RailTix";
    }
}


