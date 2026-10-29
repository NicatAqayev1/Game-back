using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebApplication1.Data;
using WebApplication1.Models;

//GET / POST Ayrı - seçkiliyi və Form/Query Birləşdirməsi
// DDoS Qoruması və Stream Buffering (64KB limit)
// Casino IP Whitelisting Təhlükəsizliyi
// Idempotency və Race Condition Qoruması (DB Unique Index + Catch)
// Maliyyə State Machine (reg, ftd, ftd_cancel)
// Tam Audit Tarixçəsi (Həm uğurlu, həm də uğursuz loglar)


[Route("api/postback")]
[ApiController]
public class PostbackController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PostbackController> _logger;

    public PostbackController(AppDbContext context, ILogger<PostbackController> logger)
    {
        _context = context;
        _logger = logger;
    }
    [HttpGet("{casinoSlug}")]
    public async Task<IActionResult> HandleGetPostback(
        string casinoSlug,
        [FromQuery] string? token,
        [FromQuery] string? type,
        [FromQuery] string? payout,
        [FromQuery] string? txId = null)
    {
        string normalizedSlug = casinoSlug?.ToLowerInvariant()?.Trim() ?? string.Empty;
        var rawData = HttpContext.Request.QueryString.Value ?? string.Empty;


        return await ProcessPostbackAsync(normalizedSlug, token, type, payout, txId, rawData);
    }
    [HttpPost("{casinoSlug}")]
    [RequestSizeLimit(1024 * 64)]
    public async Task<IActionResult> HandlePostpostback(
    string casinoSlug,
    [FromQuery] string? qToken,
    [FromQuery] string? qType,
    [FromQuery] string? qPayout,
    [FromForm] string? fPayout,
    [FromQuery] string? qTxId = null,
    [FromForm] string? fToken = null,
    [FromForm] string? fType = null,
    [FromForm] string? fTxId = null)
    {
        // 1. Prioritet olaraq Formadan gələni götürürük, yoxdursa Query-dən gələni
        var token = fToken ?? qToken;
        var type = fType ?? qType;
        var payout = fPayout ?? qPayout; // 👈 Artıq təmiz string olaraq seçildi!
        var txId = fTxId ?? qTxId;

        // 2. RawData-nı oxuyuruq
        HttpContext.Request.EnableBuffering();
        using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
        var bodyString = await reader.ReadToEndAsync();
        HttpContext.Request.Body.Position = 0;

        string normalizedSlug = casinoSlug?.ToLowerInvariant()?.Trim() ?? string.Empty;
        var rawData = $"Query: {HttpContext.Request.QueryString} | Body: {bodyString}";

        // 3. ProcessPostbackAsync metoduna string olaraq ötürürük
        return await ProcessPostbackAsync(normalizedSlug, token, type, payout, txId, rawData);
    }
    private async Task<IActionResult> ProcessPostbackAsync(
        string casinoSlug,string? token, string? type,
        string? payout, string? txId, string rawData)
    {
        //İlkin tənzimləmələr
        var clientIp = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0]?.Trim()
                       ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                       ?? "Unknown";

        string normalizedType = type?.ToLowerInvariant()?.Trim() ?? string.Empty;
        string normalizedSlug = casinoSlug?.ToLowerInvariant()?.Trim() ?? string.Empty;
        string cleanTxId = txId?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(rawData) && rawData.Length > 2000)
        {
            rawData = rawData.Substring(0, 2000);
        }

        // 1. Kazino yoxlaması
        var casino = await _context.Casinos.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == normalizedSlug);
        if (casino == null)
        {
            await SaveFailedPostbackAsync(
                 casinoId: null,                            
                 clickLogId: null,                          
                 type: normalizedType,                      
                 payout: 0,                                 
                 rawData: rawData,                         
                 ipAddress: clientIp,                      
                 txId: cleanTxId,                          
                 errorMessage: $"Invalid casino slug: {normalizedSlug}");
            _logger.LogWarning("Naməlum kazinodan postback cəhdi. Slug: {CasinoSlug}, IP: {ClientIp}", casinoSlug, clientIp);
            return Ok(new { status = "error", message = "Casino not found" });
        }

        // 2. Payout formatı və mənfi olub-olmaması yoxlaması
        if (!decimal.TryParse(payout, out decimal parsedPayout) || parsedPayout < 0)
        {
            await SaveFailedPostbackAsync(
                 casinoId: casino.Id,
                 clickLogId: null,
                 type: normalizedType,
                 payout: 0,
                 rawData: rawData,
                 ipAddress: clientIp,
                 txId: cleanTxId,
                 errorMessage: $"Invalid or negative payout format: {payout}");
            return BadRequest(new { status = "error", message = "Invalid or negative payout format" });
        }

        // 3. IP Whitelisting yoxlaması
        if (!string.IsNullOrEmpty(casino.AllowedIPs))
        {
            var allowedIps = casino.AllowedIPs.Split(',').Select(ip => ip.Trim()).ToList();
            if (!allowedIps.Contains(clientIp) && clientIp != "Unknown")
            {
                _logger.LogWarning("Kazino IP uyğunsuzluğu! Casino: {CasinoSlug}, Gələn IP: {ClientIp}", casinoSlug, clientIp);
                await SaveFailedPostbackAsync(
                   casinoId: casino.Id,
                   clickLogId: null,
                   type: normalizedType,
                   payout: parsedPayout,
                   rawData: rawData,
                   ipAddress: clientIp,
                   txId: cleanTxId,
                   errorMessage: $"IP not allowed: {clientIp}");
                return StatusCode(403, new { status = "error", message = "IP not allowed" });
            }
        }

        // 3. Idempotency yoxlaması
        if (!string.IsNullOrEmpty(cleanTxId))
        {
            bool alreadyProcessed = await _context.PostbackLogs
                .AsNoTracking()
                .AnyAsync(p => p.TransactionId == cleanTxId && p.CasinoId == casino.Id && p.IsSuccessful);

            if (alreadyProcessed)
            {
                return Ok(new { status = "success", message = "Duplicate postback ignored by TxId" });
            }
        }
        else
        {
            await SaveFailedPostbackAsync(
                 casinoId: casino.Id,
                 clickLogId: null,
                 type: normalizedType,
                 payout: parsedPayout,
                 rawData: rawData,
                 ipAddress: clientIp,
                 txId: cleanTxId,
                 errorMessage: "Transaction ID (txId) is required");
            return BadRequest(new { status = "error", message = "Transaction ID (txId) is required" });
        }


        // Token format yoxlaması
        if (!Guid.TryParse(token, out var parsedToken))
        {
            await SaveFailedPostbackAsync(
                 casinoId: casino.Id,
                 clickLogId: null,
                 type: normalizedType,
                 payout: parsedPayout,
                 rawData: rawData,
                 ipAddress: clientIp,
                 txId: cleanTxId,
                 errorMessage: "Invalid token format"); 
            return Ok(new { status = "error", message = "Invalid token format" });
        }

        ClickLog? clickLog = await _context.ClickLogs
                .FirstOrDefaultAsync(c => c.Token == parsedToken && c.CasinoId == casino.Id);

        if (clickLog == null)
        {
            await SaveFailedPostbackAsync(
                 casinoId: casino.Id,
                 clickLogId: null,
                 type: normalizedType,
                 payout: parsedPayout,
                 rawData: rawData,
                 ipAddress: clientIp,
                 txId: cleanTxId,
                 errorMessage: "Click token not found");
            return Ok(new { status = "error", message = "Click token not found" });
        }

        // ⏱️ 1. Time Validation (Tracking Window Yoxlaması)
        const int maxTrackingDays = 30;
        if (clickLog.ClickTime < DateTime.UtcNow.AddDays(-maxTrackingDays))
        {
            await SaveFailedPostbackAsync(
                casinoId: casino.Id,
                clickLogId: clickLog.Id,
                type: normalizedType,
                payout: parsedPayout,
                rawData: rawData,
                ipAddress: clientIp,
                txId: cleanTxId,
                errorMessage: "Click expired (tracking window exceeded)");
            return BadRequest(new { status = "error", message = "Tracking window has expired for this click" });
        }
        // 4. Tranzaksiya bloku
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {

            // State Machine & Guards
            switch (normalizedType)
            {
                case "reg":
                    if (!clickLog.IsDeposited && !clickLog.IsRegistered)
                    {
                        clickLog.IsRegistered = true;
                        clickLog.Payout = parsedPayout;
                    }
                    break;
                case "ftd":
                    if (!clickLog.IsDeposited)
                    {
                        clickLog.IsRegistered = true;
                        clickLog.IsDeposited = true;
                        clickLog.Payout = parsedPayout;
                    }
                    break;
                case "ftd_cancel":
                    if (clickLog.IsDeposited)
                    {
                        clickLog.IsDeposited = false;
                        clickLog.Payout = 0.0m;
                    }
                    break;
                default:
                    _logger.LogWarning("Naməlum postback tipi. Type: {Type}, Casino: {CasinoSlug}", normalizedType, normalizedSlug);
                    await SaveFailedPostbackAsync(
                           casinoId: casino.Id,
                           clickLogId: clickLog.Id,
                           type: normalizedType,
                           payout: parsedPayout,
                           rawData: rawData,
                           ipAddress: clientIp,
                           txId: cleanTxId,
                           errorMessage: $"Naməlum postback tipi {normalizedType}");
                    break;
            }

            var postbackLog = new PostbackLog
            {
                ClickLogId = clickLog.Id,
                CasinoId = casino.Id,
                PostbackType = normalizedType,
                Payout = parsedPayout,
                RawData = rawData ?? string.Empty,
                IpAddress = clientIp,
                TransactionId = cleanTxId,
                IsSuccessful = true,
                ResponseMessage = "Successfully",
                ProcessedTime = DateTime.UtcNow
            };

            _context.PostbackLogs.Add(postbackLog);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { status = "success", message = "Processed successfully" });
        }
        catch (DbUpdateException dbEx) when (IsDuplicateKeyException(dbEx))
        {
            await transaction.RollbackAsync();
            _logger.LogWarning("Race condition zamanı dublikat postback bloklandı. TxId: {TxId}", txId);
            return Ok(new { status = "success", message = "Duplicate postback caught by DB constraint" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Postback emal edilərkən xəta baş verdi. Token: {Token}", token);

            // İndi clickLog dəyişəni try səviyyəsində elan olunduğu üçün catch-də əlçatandır
            await SaveFailedPostbackAsync(
                           casinoId: casino.Id,
                           clickLogId: clickLog.Id,
                           type: normalizedType,
                           payout: parsedPayout,
                           rawData: rawData,
                           ipAddress: clientIp,
                           txId: cleanTxId,
                           errorMessage: "Internal processing error");
            return Ok(new { status = "error", message = "Internal error handled" });
        }
    }
    //Köməkçi metodlar
    private bool IsDuplicateKeyException(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number == 2627 || sqlEx.Number == 2601;
        }

        return ex.InnerException?.Message.Contains("UNIQUE") == true ||
               ex.InnerException?.Message.Contains("IX_") == true;
    }
    private async Task SaveFailedPostbackAsync(
        int? casinoId, long? clickLogId, string type, decimal payout,
        string rawData, string ipAddress, string? txId, string errorMessage)
    {
        try
        {
            var log = new PostbackLog
            {
                CasinoId = casinoId,
                ClickLogId = clickLogId,
                PostbackType = type,
                Payout = payout,
                RawData = rawData,
                IpAddress = ipAddress,
                TransactionId = txId,
                IsSuccessful = false,
                ResponseMessage = errorMessage,
                ProcessedTime = DateTime.UtcNow
            };

            _context.PostbackLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uğursuz postback logunu bazaya yazarkən xəta baş verdi.");
        }
    }
}