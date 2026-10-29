namespace WebApplication1.Models
{
    public class PostbackLog
    {
        public long Id { get; set; }
        public long? ClickLogId { get; set; }
        public ClickLog ClickLog { get; set; } = null!;
        public int? CasinoId { get; set; }
        public string PostbackType { get; set; } = string.Empty;
        public decimal Payout { get; set; }
        public string RawData { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ResponseMessage { get; set; }
        public DateTime ProcessedTime { get; set; } = DateTime.UtcNow;
    }
}
