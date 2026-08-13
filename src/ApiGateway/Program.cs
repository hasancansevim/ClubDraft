using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ─── CORS ────────────────────────────────────────────────────────────────────
// Named policy — hem tarayıcı preflight (OPTIONS) hem de gerçek istekler için.
// SetIsOriginAllowed(_ => true) yerine WithOrigins kullanıyoruz:
// AllowCredentials() ile birlikte wildcard (*) geçersiz olduğu için.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// ─── YARP Reverse Proxy ───────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Sıralama kritik:
// 1. UseRouting  — istek hangi route'a gidecek belirlenir
// 2. UseCors     — CORS headers eklenir / OPTIONS short-circuit edilir
// 3. UseWebSockets — WebSocket upgrade (SignalR için)
// 4. MapReverseProxy — eşleşen istekler upstream'e iletilir
app.UseRouting();

app.UseCors("FrontendPolicy");

app.UseWebSockets();

// YARP pipeline'ına da CORS middleware'i bağla.
// Aksi hâlde YARP kendi pipeline'ında preflight'ı upstream'e iletip
// oradaki cevabı (CORS headers içermeyebilir) tarayıcıya döndürebilir.
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseCors("FrontendPolicy");
});

app.Run();
