using ClubCraft.Session.Domain.Aggregates;
using ClubCraft.Session.Infrastructure.Persistence;
using ClubCraft.Session.Infrastructure.Persistence;
using ClubCraft.Session.API.Consumers;
using Microsoft.EntityFrameworkCore;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<SessionDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<SessionDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<DraftCompletedEventConsumer>();
    x.AddConsumer<WeekSimulationCompletedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        
        cfg.ReceiveEndpoint("session-events", e =>
        {
            e.UseEntityFrameworkOutbox<SessionDbContext>(context);
            e.ConfigureConsumer<DraftCompletedEventConsumer>(context);
            e.ConfigureConsumer<WeekSimulationCompletedEventConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Minimal APIs
app.MapPost("/api/sessions", async (CreateSessionRequest request, SessionDbContext dbContext) =>
{
    var gameRoom = GameRoom.Create(request.HostUserId, request.MaxParticipants);
    dbContext.GameRooms.Add(gameRoom);
    await dbContext.SaveChangesAsync();
    return Results.Ok(new { gameRoom.Id });
});

app.MapPost("/api/sessions/{id:guid}/join", async (Guid id, JoinSessionRequest request, SessionDbContext dbContext) =>
{
    var gameRoom = await dbContext.GameRooms.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id);
    if (gameRoom is null) return Results.NotFound();

    try
    {
        gameRoom.Join(request.UserId, request.ClubName);
        await dbContext.SaveChangesAsync();
        return Results.Ok();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/sessions/{id:guid}/ready", async (Guid id, ReadySessionRequest request, SessionDbContext dbContext, HttpContext httpContext) =>
{
    var publishEndpoint = httpContext.RequestServices.GetRequiredService<IPublishEndpoint>();
    var gameRoom = await dbContext.GameRooms.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id);
    if (gameRoom is null) return Results.NotFound();

    try
    {
        gameRoom.MarkReady(request.ParticipantId, request.Phase);
        
        var domainEvents = gameRoom.DomainEvents.ToList();
        gameRoom.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is ClubCraft.Session.Domain.Events.AllParticipantsReadyForDraftEvent draftReadyEvent)
            {
                await publishEndpoint.Publish<ClubCraft.BuildingBlocks.Contracts.Events.IAllParticipantsReadyForDraftEvent>(new
                {
                    RoomId = draftReadyEvent.RoomId,
                    ParticipantClubIds = draftReadyEvent.ParticipantClubIds
                });
            }
            else if (domainEvent is ClubCraft.Session.Domain.Events.AllParticipantsReadyForNextWeekEvent nextWeekEvent)
            {
                await publishEndpoint.Publish<ClubCraft.BuildingBlocks.Contracts.Events.IAllParticipantsReadyForNextWeekEvent>(new
                {
                    RoomId = nextWeekEvent.RoomId,
                    Week = nextWeekEvent.Week
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return Results.Ok();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/sessions/{id:guid}/advance-to-draft", async (Guid id, SessionDbContext dbContext) =>
{
    var gameRoom = await dbContext.GameRooms.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id);
    if (gameRoom is null) return Results.NotFound();

    gameRoom.AdvanceToDraft();
    await dbContext.SaveChangesAsync();
    return Results.Ok();
});

app.Run();

public record CreateSessionRequest(string HostUserId, int MaxParticipants);
public record JoinSessionRequest(string UserId, string ClubName);
public record ReadySessionRequest(Guid ParticipantId, string Phase);
