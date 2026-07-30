using ClubCraft.Draft.Application.Behaviors;
using ClubCraft.Draft.Application.Commands.StartDraft;
using ClubCraft.Draft.Application.Repositories;
using ClubCraft.Draft.Infrastructure.Behaviors;
using ClubCraft.Draft.Infrastructure.Persistence;
using ClubCraft.Draft.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using ClubCraft.Draft.Application.Commands.ClaimPlayer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 1. EF Core & Database ---
builder.Services.AddDbContext<DraftDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DraftDb"));
});

// --- 2. Repositories ---
builder.Services.AddScoped<IDraftSessionRepository, DraftSessionRepository>();

// --- 3. MediatR & FluentValidation Pipeline ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(StartDraftCommand).Assembly);
    
    // Validation
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Redis Lock Behavior FIRST, so we lock before validating/handling. Since it's closed, we register directly:
builder.Services.AddTransient<IPipelineBehavior<ClaimPlayerCommand, ClaimPlayerResult>, DraftLockBehavior>();

builder.Services.AddValidatorsFromAssembly(typeof(StartDraftCommandValidator).Assembly);

// --- 4. StackExchange.Redis ---
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(config!);
});

// --- 5. MassTransit & RabbitMQ with EF Core Outbox ---
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<DraftDbContext>(o =>
    {
        // Use Postgres specifically for the outbox lock statements
        o.UsePostgres();
        o.UseBusOutbox();
    });
    x.AddConsumer<ClubCraft.Draft.API.Consumers.ReleasePlayerClaimCommandConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        
        cfg.ReceiveEndpoint("draft-commands", e =>
        {
            e.ConfigureConsumer<ClubCraft.Draft.API.Consumers.ReleasePlayerClaimCommandConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
