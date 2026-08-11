using Microsoft.EntityFrameworkCore;
using MassTransit;
using ClubCraft.ClubManagement.Application;
using ClubCraft.ClubManagement.Infrastructure;
using ClubCraft.ClubManagement.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure("Host=127.0.0.1;Port=5435;Database=clubmanagement;Username=clubcraft;Password=clubcraft;");

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<ClubCraft.ClubManagement.Infrastructure.Persistence.ClubDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<AddPlayerToRosterCommandConsumer>();
    x.AddConsumer<ReleasePlayerFromRosterCommandConsumer>();
    x.AddConsumer<ClubCraft.ClubManagement.Application.Consumers.SponsorshipAcceptedEventConsumer>();
    x.AddConsumer<ClubCraft.ClubManagement.Application.Consumers.ParticipantJoinedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("127.0.0.1", "/", h =>
        {
            h.Username("clubcraft");
            h.Password("clubcraft");
        });

        cfg.ReceiveEndpoint("club-management-commands", e =>
        {
            // Outbox zorunlu: AddPlayerToRoster işledikten sonra
            // PlayerAddedToRosterEvent'in Saga'ya atomik olarak iletilmesi için
            e.UseEntityFrameworkOutbox<ClubCraft.ClubManagement.Infrastructure.Persistence.ClubDbContext>(context);
            e.ConfigureConsumer<AddPlayerToRosterCommandConsumer>(context);
            e.ConfigureConsumer<ReleasePlayerFromRosterCommandConsumer>(context);
        });

        cfg.ReceiveEndpoint("club-management-events", e =>
        {
            e.UseEntityFrameworkOutbox<ClubCraft.ClubManagement.Infrastructure.Persistence.ClubDbContext>(context);
            e.ConfigureConsumer<ClubCraft.ClubManagement.Application.Consumers.SponsorshipAcceptedEventConsumer>(context);
            e.ConfigureConsumer<ClubCraft.ClubManagement.Application.Consumers.ParticipantJoinedEventConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
