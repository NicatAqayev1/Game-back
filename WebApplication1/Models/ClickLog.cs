using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Index(nameof(Token), IsUnique = true)] 
    [Index(nameof(ClickTime))]
    public class ClickLog
    {
        public long Id { get; set; }
        public Guid Token { get; set; } = Guid.NewGuid();
        public int? CasinoId { get; set; }
        public virtual Casino? Casino { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "XX"; // Məsələn: AZ, TR, RU
        public string? UserAgent { get; set; }
        public DateTime ClickTime { get; set; } = DateTime.UtcNow;
        public bool IsRegistered { get; set; } = false; // Qeydiyyat (Reg) olubmu?
        public bool IsDeposited { get; set; } = false;  // İlk depozit (FTD) olubmu?
        [NotMapped]
        public bool IsConverted => IsRegistered || IsDeposited;
        [Precision(18, 2)]
        public decimal Payout { get; set; } = 0.0m;     // Bu klikdən əldə olunan gəlir (AZN/USD)
        public string? Sub1 { get; set; } // Məsələn: trafik mənbəyi (telegram, facebook)
        public string? Sub2 { get; set; } // Məsələn: kampaniya adı (summer_promo)
        public string? Sub3 { get; set; } // Məsələn: kreativ / banner növü
    }
}
