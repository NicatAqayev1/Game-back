namespace WebApplication1.Models
{
    public class Casino
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;          // Məsələn: 1xBet Kazino
        public string Slug { get; set; } = string.Empty;          // URL üçün: 1xbet
        public string LogoText { get; set; } = string.Empty;      // Loqo qısaltması: 1X
        public string LogoColor { get; set; } = string.Empty;     // CSS rəng kodu
        public string Rating { get; set; } = string.Empty;        // 4.9
        public string BonusTitle { get; set; } = string.Empty;    // Xoş Gəldin Paketi
        public string BonusValue { get; set; } = string.Empty;    // 1500 EUR + 150 FS
        public string PromoCode { get; set; } = string.Empty;     // AZBONUS1
        public string AffiliateUrl { get; set; } = string.Empty;  // Sizin real affiliate linkiniz
        public string Categories { get; set; } = string.Empty;    // slots live instant
        public bool IsFeatured { get; set; }      // Ayın təklifidirmi?
    }
}
