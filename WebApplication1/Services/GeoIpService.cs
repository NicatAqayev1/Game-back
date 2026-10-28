using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface IGeoIpService
    {
        Task<string> GetCountryCodeAsync(string ipAddress);
    }

    public class GeoIpService : IGeoIpService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public GeoIpService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<string> GetCountryCodeAsync(string ipAddress)
        {
            // Localhost / Yerli IP yoxlaması
            if (ipAddress == "127.0.0.1" || ipAddress == "::1" || ipAddress == "unknown" || ipAddress.StartsWith("192.168."))
            {
                return "AZ"; // Test üçün yerli IP-ləri Azərbaycan qəbul edirik
            }

            string cacheKey = $"geo_{ipAddress}";
            // Əgər bu IP daha əvvəl yoxlanılıbsa, keşdən oxuyuruq (API-ni yormamaq üçün)
            if (_cache.TryGetValue(cacheKey, out string cachedCountry))
            {
                return cachedCountry;
            }

            try
            {
                // Pulsuz ip-api xidmətindən istifadə edirik
                var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}?fields=status,countryCode");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.GetProperty("status").GetString() == "success")
                    {
                        string countryCode = root.GetProperty("countryCode").GetString() ?? "XX";

                        // Bu IP-ni 7 gün müddətinə yadda saxlayırıq
                        _cache.Set(cacheKey, countryCode, TimeSpan.FromDays(7));
                        return countryCode;
                    }
                }
            }
            catch
            {
                // Xəta baş verərsə sistem dayanmasın
            }

            return "XX";
        }
    }
}