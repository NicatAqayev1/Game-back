using System.Collections.Generic;

namespace WebApplication1.Models.ViewModels
{
    public class AnalyticsViewModel
    {
        // Kliklərin siyahısı (Null qalmaması üçün ilkin dəyər veririk)
        public IEnumerable<ClickLog> Logs { get; set; } = Enumerable.Empty<ClickLog>();

        // Əsas KPI-lar
        public int TotalClicks { get; set; }
        public int UniqueClicks { get; set; }
        public int TotalRegistrations { get; set; } // Reg sayı
        public int TotalDeposits { get; set; }      // FTD sayı

        public double ConversionRate { get; set; }  // FTD-yə əsaslanan CR%
        public decimal TotalPayout { get; set; }

        // İstehsalat üçün əlavə faydalı filtrlər (Dashboard-da tarix seçimi üçün)
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? SelectedCasinoId { get; set; } // Müəyyən kazinoya görə filtr
    }
}