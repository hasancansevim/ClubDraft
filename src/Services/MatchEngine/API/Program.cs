using ClubCraft.MatchEngine.Application.Commands.GenerateFixture;
using ClubCraft.MatchEngine.Application.Consumers;
using ClubCraft.MatchEngine.Domain.Services;
using ClubCraft.MatchEngine.Infrastructure;
using ClubCraft.MatchEngine.Infrastructure.Persistence;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GenerateFixtureCommand).Assembly));

// Infrastructure (DbContext, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Domain Services
builder.Services.AddTransient<IFixtureGenerator, RoundRobinFixtureGenerator>();
builder.Services.AddTransient<IMatchSimulator, MatchSimulator>();
builder.Services.AddTransient<IClubPowerCalculator, ClubPowerCalculator>();

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<MatchEngineDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<DraftCompletedEventConsumer>();
    x.AddConsumer<AllParticipantsReadyForNextWeekEventConsumer>();
    x.AddConsumer<PlayerAddedToRosterCommandConsumer>();
    x.AddConsumer<PlayerRemovedFromRosterCommandConsumer>();
    x.AddConsumer<WeeklyDecisionMadeEventConsumer>();
    x.AddConsumer<LineupUpdatedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("match-engine-events", e =>
        {
            e.UseEntityFrameworkOutbox<MatchEngineDbContext>(context);
            e.ConfigureConsumer<DraftCompletedEventConsumer>(context);
            e.ConfigureConsumer<AllParticipantsReadyForNextWeekEventConsumer>(context);
            e.ConfigureConsumer<PlayerAddedToRosterCommandConsumer>(context);
            e.ConfigureConsumer<PlayerRemovedFromRosterCommandConsumer>(context);
            e.ConfigureConsumer<WeeklyDecisionMadeEventConsumer>(context);
            e.ConfigureConsumer<LineupUpdatedEventConsumer>(context);
        });
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
