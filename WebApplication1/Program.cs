using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
// DbContext Qeydiyyatı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true; // XSS hücumlarından qoruyur
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS-i dəstəkləyir
        options.Cookie.SameSite = SameSiteMode.Strict; // CSRF hücumlarının qarşısını alır
    });
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<WebApplication1.Services.IGeoIpService, WebApplication1.Services.GeoIpService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed data vasitəsilə ilk başlanğıc məlumatlarını əlavə etmək
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated(); // Baza yoxdursa avtomatik yaradır

    // Əgər bazada kazino yoxdursa, başlanğıc verilənlərini doldururuq
    //if (!context.Casinos.Any())
    //{
    //    context.Casinos.AddRange(
    //        new Casino { Name = "1xBet Kazino", Slug = "1xbet", LogoText = "1X", LogoColor = "var(--purple)", Rating = "4.9", BonusTitle = "Xoş Gəldin Paketi", BonusValue = "1500 EUR + 150 FS", PromoCode = "AZBONUS1", AffiliateUrl = "https://example.com/affiliate-1xbet", Categories = "slots live instant", IsFeatured = true },
    //        new Casino { Name = "Mostbet AZ", Slug = "mostbet", LogoText = "MS", LogoColor = "var(--neon-green)", Rating = "4.8", BonusTitle = "İlk Depozit Bonusu", BonusValue = "550 AZN + 250 FS", PromoCode = "MOSTVIP", AffiliateUrl = "https://example.com/affiliate-mostbet", Categories = "sports instant", IsFeatured = false },
    //        new Casino { Name = "Parimatch AZ", Slug = "parimatch", LogoText = "PM", LogoColor = "var(--neon-cyan)", Rating = "4.7", BonusTitle = "VIP Kazino Paketi", BonusValue = "1000 AZN + 100 FS", PromoCode = "PARIBONUS", AffiliateUrl = "https://example.com/affiliate-parimatch", Categories = "slots live", IsFeatured = false }
    //    );
    //    context.SaveChanges();
    //}
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Canlı mühitdə xəta səhifəsinə yönləndirir
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}
app.UseForwardedHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();