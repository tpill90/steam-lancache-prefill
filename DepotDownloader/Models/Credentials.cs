namespace DepotDownloader.Models
{
    public class Credentials
    {
        public bool LoggedOn { get; set; }

        public bool IsValid => LoggedOn;
    }
}