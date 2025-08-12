using Fido2NetLib;
using WebAuthn2.Data;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Fido2 ve DevelopmentInMemoryStore 
builder.Services.AddSingleton<InMemoryUserStore>();
builder.Services.AddSingleton<Fido2Configuration>(config =>
{
    var fido2Config = builder.Configuration.GetSection("Fido2").Get<Fido2Configuration>();
    // Origin'i ve diðer ayarlarý kendi uygulamanýza göre yapýlandýrýn
    // Uzantýnýzýn çalýþtýðý sayfa (örneðin webauthn.io) ve API'nizin adresi
    fido2Config.Origins = new HashSet<string>
    {
        "https://webauthn.io",
        "https://localhost:5271", 
        "http://localhost:7132"
    };
    fido2Config.ServerDomain = "webauthn.io"; // Genellikle sitenizin domaini
    fido2Config.ServerName = "WebAuthn Uzantý Uygulamasý";
    return fido2Config;
});
builder.Services.AddScoped<Fido2NetLib.Fido2>();


builder.Services.AddDistributedMemoryCache(); // Oturum verilerini bellekte tutmak için
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5); // Oturum süresi
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None; // Cross-origin istekler için None
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS kullanýyorsanýz Always olmalý
});

// CORS 
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(
                      "https://webauthn.io",
                      "chrome-extension://jbkfeeihfjmfailgienlplapgfaacfjn"
                  )
                  .AllowAnyHeader() 
                  .AllowAnyMethod()  
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapOpenApi();
}

app.UseCors();

app.UseSession();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
