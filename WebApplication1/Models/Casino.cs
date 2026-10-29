using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    // Slug unikal olmalıdır ki, eyni adla iki fərqli URL olmasın
    [Index(nameof(Slug), IsUnique = true)]
    public class Casino
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;          // Məsələn: 1xBet Kazino

        [Required]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;          // URL üçün: 1xbet

        public string LogoText { get; set; } = string.Empty;      // Loqo qısaltması: 1X
        public string LogoColor { get; set; } = string.Empty;     // CSS rəng kodu

        [Precision(3, 1)] // Məsələn: 4.9 (3 rəqəm ümumi, 1 rəqəm nöqtədən sonra)
        public decimal Rating { get; set; } = 0.0m;        // Rəqəmsal etdik ki, sıralamaq (sorting) mümkün olsun

        public string BonusTitle { get; set; } = string.Empty;    // Xoş Gəldin Paketi
        public string BonusValue { get; set; } = string.Empty;    // 1500 EUR + 150 FS

        public string? PromoCode { get; set; }     // Hər kazinoda promo kod olmaya bilər, nullable edirik

        [Required]
        public string AffiliateUrl { get; set; } = string.Empty;  // Sizin real affiliate linkiniz

        public string Categories { get; set; } = string.Empty;    // slots live instant
        public bool IsFeatured { get; set; } = false;      // Ayın təklifidirmi?
        public string? AllowedIPs { get; set; }

        // Navigation Property (Bu kazinoya aid olan kliklərin siyahısı)
        public virtual ICollection<ClickLog> ClickLogs { get; set; } = new List<ClickLog>();
    }
}
