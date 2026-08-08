using ClubCraft.RealtimeHub.API.Consumers;
using ClubCraft.RealtimeHub.API.Hubs;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR
builder.Services.AddSignalR();

// Add CORS (Very important for SignalR clients)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EventConsumers>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // Tüm eventleri dinleyen merkezi hub kuyruğu
        cfg.ReceiveEndpoint("realtimehub-events", e =>
        {
            e.ConfigureConsumer<EventConsumers>(context);
        });
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/", () => "ClubCraft RealtimeHub API");

app.MapHub<GameHub>("/gameHub");

app.Run();
