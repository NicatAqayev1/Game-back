using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
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
            public string status { get; set; }
            public string countryCode { get; set; }
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
                _logger.LogWarning(ex, "Geo-IP xidmətinə qoşulmaq mümkün olmadı. IP: {IpAddress}. Fallback ölkə tətbiq edildi.", ipAddress);
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

        // 2. Postback Endpoint (Reg və FTD dəstəyi ilə)
        // URL nümunəsi (Reg): /api/postback?clickId=5&type=reg
        // URL nümunəsi (FTD): /api/postback?clickId=5&type=ftd&payout=100.00
        [HttpGet("api/postback")]
        public async Task<IActionResult> Postback([FromQuery] Guid token, [FromQuery] string type, [FromQuery] decimal payout, [FromQuery] string secret = "")
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string fullUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;

            bool isSuccess = false;
            string message = "";
            string validSecret = _configuration["PostbackSecret"];

            // 1. Secret Key yoxlaması
            if (string.IsNullOrEmpty(validSecret) || secret != validSecret)
            {
                message = "İcazəsiz giriş! Keçərsiz Secret Key.";
                await LogPostback(fullUrl, clientIp, token.ToString(), type, payout, false, message);
                return Unauthorized(new { success = false, message });
            }

            // 2. Token yoxlaması
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clickLog = await _context.ClickLogs.FirstOrDefaultAsync(c => c.Token == token);
                if (clickLog == null)
                {
                    message = "Token tapılmadı və ya etibarsızdır!";
                    await LogPostback(fullUrl, clientIp, token.ToString(), type, payout, false, message);
                    return NotFound(new { success = false, message });
                }

                TimeSpan attributionWindow = TimeSpan.FromDays(30); // Standart iGaming pəncərəsi
                if (DateTime.Now - clickLog.ClickTime > attributionWindow)
                {
                    message = $"Atributsiya pəncərəsi bitib! Klik tarixi: {clickLog.ClickTime}, Limit 30 gündür.";
                    await LogPostback(fullUrl, clientIp, token.ToString(), type, payout, false, message);
                    return BadRequest(new { success = false, message });
                }
                // 3. Type yoxlaması
                if (string.IsNullOrEmpty(type))
                {
                    message = "Konversiya növü (type) göstərilməlidir!";
                    await LogPostback(fullUrl, clientIp, token.ToString(), type, payout, false, message);
                    return BadRequest(new { success = false, message });
                }

                string actionType = type?.ToLower() ?? "reg";

                if (actionType == "reg")
                {
                    clickLog.IsRegistered = true;
                }
                else if (actionType == "ftd")
                {
                    clickLog.IsRegistered = true; // FTD edən şəxs onsuz da qeydiyyatdan keçib
                    clickLog.IsDeposited = true;
                    clickLog.Payout += payout;
                }
                else
                {
                    message = "Yanlış növ! 'reg' və ya 'ftd' yazın.";
                    await LogPostback(fullUrl, clientIp, token.ToString(), type, payout, false, message);
                    return BadRequest(new { success = false, message });
                }
                _context.PostbackLogs.Add(new PostbackLog
                {
                    ReceivedAt = DateTime.Now,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    Type = actionType,
                    Payout = payout,
                    IsSuccessful = true,
                    ResponseMessage = "Processed successfully via transaction"
                });
                await _context.SaveChangesAsync();
                isSuccess = true;
                message = $"Uğurla yeniləndi! Növ: {actionType.ToUpper()}, Payout: {clickLog.Payout}";

                // Uğurlu sorğunu loglayırıq
                await LogPostback(fullUrl, clientIp, token.ToString(), type, payout, true, message);

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Xəta olarsa heç nəyi dəyişmirik
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        // Köməkçi loglama metodu
        private async Task LogPostback(string url, string ip, string token, string type, decimal payout, bool success, string responseMsg)
    {
        var postbackLog = new PostbackLog
        {
            ReceivedAt = DateTime.Now,
            RequestUrl = url,
            IpAddress = ip,
            RawToken = token,
            Type = type ?? "unknown",
            Payout = payout,
            IsSuccessful = success,
            ResponseMessage = responseMsg
        };

        _context.PostbackLogs.Add(postbackLog);
        await _context.SaveChangesAsync();
    }
        
    }
}