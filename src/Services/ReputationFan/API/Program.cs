using ClubCraft.ReputationFan.Application.Consumers;
using ClubCraft.ReputationFan.Infrastructure;
using ClubCraft.ReputationFan.Infrastructure.Persistence;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (DbContext, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ClubCraft.ReputationFan.Application.Queries.GetReputation.GetReputationQuery).Assembly));

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<ReputationDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<PlayerAddedToRosterEventConsumer>();
    x.AddConsumer<WeeklyDecisionMadeEventConsumer>();
    x.AddConsumer<MatchSimulatedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("reputation-events", e =>
        {
            e.UseEntityFrameworkOutbox<ReputationDbContext>(context);
            e.ConfigureConsumer<PlayerAddedToRosterEventConsumer>(context);
            e.ConfigureConsumer<WeeklyDecisionMadeEventConsumer>(context);
            e.ConfigureConsumer<MatchSimulatedEventConsumer>(context);
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
