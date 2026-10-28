using System.Collections.Generic;

namespace WebApplication1.Models.ViewModels
{
    public class AnalyticsViewModel
    {
        public IEnumerable<ClickLog> Logs { get; set; }
        public int TotalClicks { get; set; }
        public int UniqueClicks { get; set; }
        public int TotalRegistrations { get; set; } // Reg sayı
        public int TotalDeposits { get; set; }      // FTD sayı
        public double ConversionRate { get; set; }  // FTD-yə əsaslanan CR%
        public decimal TotalPayout { get; set; }
    }
}