using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IGeoIpService _geoIpService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(AppDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<HomeController> logger, IMemoryCache memoryCache, IGeoIpService geoIpService)
        {
            _context = context;
            _memoryCache = memoryCache;
            _geoIpService = geoIpService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public class GeoApiResponse
        {
            [JsonPropertyName("status")]
            public string status { get; set; } = string.Empty;

            [JsonPropertyName("countryCode")]
            public string countryCode { get; set; } = string.Empty;
        }
        // Ana Səhifə - Bazadakı bütün aktiv kazinoları oxuyur
        public async Task<IActionResult> Index()
        {
            var casinos = await _context.Casinos.ToListAsync();
            return View(casinos);
        }
        private bool IsBot(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return true;

            string ua = userAgent.ToLower();

            // Geniş yayılmış bot, crawler və avtomatik skript imzaları
            string[] knownBots = new[]
            {
                "bot", "crawler", "spider", "python", "curl",
                "wget", "headless", "puppeteer", "selenium",
                "googlebot", "bingbot", "yandexbot", "ahrefsbot"
            };

            foreach (var bot in knownBots)
            {
                if (ua.Contains(bot)) return true;
            }

            return false;
        }
        // Kazino İncələmə (Review) Səhifəsi
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return RedirectToAction("Index");

            var casino = await _context.Casinos.FirstOrDefaultAsync(c => c.Slug == slug);
            if (casino == null) return RedirectToAction("Index");

            return View(casino);
        }
        // İstifadəçi brauzerdə https://sizinsayt.com/go/1xbet yazdıqda və ya düyməyə kliklədikdə bu metod çalışır:
        [Route("go/{slug}")]
        public async Task<IActionResult> RedirectToCasino(string slug, string sub1, string sub2, string sub3)
        {
            var casino = await _context.Casinos.FirstOrDefaultAsync(c => c.Slug == slug);
            if (casino == null) return NotFound();

            //1. Bot yoxlaması
            string userAgent = Request.Headers["User-Agent"].ToString();

            if (IsBot(userAgent))
            {
                return Redirect(casino.AffiliateUrl);
            }
            // 2. Rate Limiting yoxlaması
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string cacheKey = $"click_limit_{ipAddress}_{casino.Id}";

            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                // Eyni IP son 10 saniyədə klikləyibsə, bazaya təkrar LOG YAZMIRIQ
                return Redirect(casino.AffiliateUrl);
            }

            // 10 saniyəlik bloklama vaxtı qoyuruq
            _memoryCache.Set(cacheKey, true, TimeSpan.FromSeconds(10));

            // 3. --- GEO-IP YOXLAMASI ---
            string countryCode = await _geoIpService.GetCountryCodeAsync(ipAddress);
            //if (countryCode == "AZ")
            //{
            //    return Content("Bu təklif sizin ölkənizdə mövcud deyil.");
            //}
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(2); // 2 saniyədən çox gözləməsin!

                // ip-api.com xidmətindən ölkəni soruşuruq
                var geoResponse = await client.GetFromJsonAsync<GeoApiResponse>($"http://ip-api.com/json/{ipAddress}?fields=status,countryCode");

                if (geoResponse != null && geoResponse.status == "success" && !string.IsNullOrEmpty(geoResponse.countryCode))
                {
                    countryCode = geoResponse.countryCode;
                }
            }
            catch (Exception ex)
            {
                // Xarici API çökərsə və ya timeout olarsa, sistem çökmyəcək!
                // Sadəcə xətanı loglayırıq və countryCode = "XX" olaraq davam edirik.
                _logger.LogWarning(ex, $"Geo-IP xidmətinə qoşulmaq mümkün olmadı. IP: {ipAddress}. Fallback ölkə tətbiq edildi.", ipAddress);
            }
            // 4. Klik qeydini yaradırıq
            var clickLog = new ClickLog
            {
                Token = Guid.NewGuid(),
                CasinoId = casino.Id,
                ClickTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CountryCode = countryCode,
                Sub1 = sub1,
                Sub2 = sub2,
                Sub3 = sub3,
                IsRegistered = false,
                IsDeposited = false,
                Payout = 0.0m
            };

            _context.ClickLogs.Add(clickLog);
            await _context.SaveChangesAsync(); // Bazaya yazılır ki, Id avtomatik yaransın!

            // 5. Affiliate linkinə bizim Click ID-ni (subid) əlavə edirik
            string separator = casino.AffiliateUrl.Contains("?") ? "&" : "?";
            string targetUrl = $"{casino.AffiliateUrl}{separator}subid={clickLog.Token}";

            // Əgər kazino da sub1-i qəbul edirsə, onu da əlavə edə bilərsiniz:
            //if (!string.IsNullOrEmpty(sub1))
            //{
            //    targetUrl += $"&sub1={Uri.EscapeDataString(sub1)}";
            //}

            return Redirect(targetUrl);
        }
    }
 }