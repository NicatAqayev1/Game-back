namespace WebApplication1.Models
{
    public class PostbackLog
    {
        public int Id { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
        public string RequestUrl { get; set; }      // Gələn tam URL
        public string IpAddress { get; set; }       // Kazino serverinin IP ünvanı
        public string RawToken { get; set; }        // Gələn token dəyəri
        public string Type { get; set; }            // reg və ya ftd
        public decimal Payout { get; set; }         // Məbləğ
        public bool IsSuccessful { get; set; }      // Uğurlu olub-olmadığı
        public string ResponseMessage { get; set; } // Bizim sistemin verdiyi cavab
    }
}
