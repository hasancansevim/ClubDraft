using ClubCraft.FinanceSponsorship.Application.Consumers;
using ClubCraft.FinanceSponsorship.Infrastructure;
using ClubCraft.FinanceSponsorship.Infrastructure.Persistence;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (DbContext, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<FinanceDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<ReputationThresholdReachedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("finance-events", e =>
        {
            e.UseEntityFrameworkOutbox<FinanceDbContext>(context);
            e.ConfigureConsumer<ReputationThresholdReachedEventConsumer>(context);
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

app.MapGet("/api/finances/{clubId}/offers", async (Guid clubId, ClubCraft.FinanceSponsorship.Application.Repositories.ISponsorshipOfferRepository repo) =>
{
    var offers = await repo.GetByClubIdAsync(clubId);
    bool anyChanged = false;
    foreach (var offer in offers)
    {
        if (offer.Status == ClubCraft.FinanceSponsorship.Domain.Aggregates.OfferStatus.Pending && offer.ExpiresAt < DateTime.UtcNow)
        {
            offer.Expire();
            await repo.UpdateAsync(offer);
            anyChanged = true;
        }
    }
    
    return Results.Ok(offers);
});

app.MapPost("/api/finances/{clubId}/offers/{offerId}/respond", async (Guid clubId, Guid offerId, OfferResponseDto dto, ClubCraft.FinanceSponsorship.Application.Repositories.ISponsorshipOfferRepository repo, IPublishEndpoint publishEndpoint) =>
{
    var offer = await repo.GetByIdAsync(offerId);
    if (offer == null || offer.ClubId != clubId) return Results.NotFound();

    try
    {
        if (dto.Response == "Accept")
            offer.Accept();
        else if (dto.Response == "Reject")
            offer.Reject();
        else
            return Results.BadRequest("Invalid response");
            
        await repo.UpdateAsync(offer);
        
        foreach (var domainEvent in offer.DomainEvents)
        {
            if (domainEvent is ClubCraft.FinanceSponsorship.Domain.Events.SponsorshipAcceptedEvent acceptedEvent)
            {
                await publishEndpoint.Publish<ClubCraft.BuildingBlocks.Contracts.Events.ISponsorshipAcceptedEvent>(new 
                {
                    ClubId = acceptedEvent.ClubId,
                    Amount = acceptedEvent.Amount
                });
            }
        }
        offer.ClearDomainEvents();
        
        return Results.Ok(offer);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();

public class OfferResponseDto
{
    public string Response { get; set; } = string.Empty;
}
