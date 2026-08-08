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
        // Not: UseBusOutbox() kaldırıldı — realtime event'ler IBus ile direkt yayınlanıyor
        // Outbox sadece consumer inbox deduplication için kullanılıyor
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
app.MapPost("/api/sessions", async (CreateSessionRequest request, SessionDbContext dbContext, IBus bus) =>
{
    var gameRoom = GameRoom.Create(request.HostUserId, request.MaxParticipants);
    dbContext.GameRooms.Add(gameRoom);
    await dbContext.SaveChangesAsync();
    
    // IBus ile direkt publish (outbox bypass) — realtime event, idempotency gerekmez
    var domainEvents = gameRoom.DomainEvents.ToList();
    gameRoom.ClearDomainEvents();
    foreach (var domainEvent in domainEvents)
    {
        if (domainEvent is ClubCraft.Session.Domain.Events.RoomCreatedEvent roomCreated)
        {
            await bus.Publish<ClubCraft.BuildingBlocks.Contracts.Events.IRoomCreatedEvent>(new
            {
                RoomId = roomCreated.RoomId,
                HostUserId = roomCreated.HostUserId
            });
        }
    }

    return Results.Ok(new { gameRoom.Id, gameRoom.ShortCode });
});

app.MapPost("/api/sessions/{id:guid}/join", async (Guid id, JoinSessionRequest request, SessionDbContext dbContext, IBus bus) =>
{
    var gameRoom = await dbContext.GameRooms.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id);
    if (gameRoom is null) return Results.NotFound();

    try
    {
        var participantId = gameRoom.Join(request.UserId, request.ClubName);
        var domainEvents = gameRoom.DomainEvents.ToList();
        gameRoom.ClearDomainEvents();

        await dbContext.SaveChangesAsync();
        Console.WriteLine($"[JOIN] SaveChanges OK. Publishing {domainEvents.Count} domain events...");

        // IBus ile direkt publish (outbox bypass) — SaveChanges sonrası, kayıt güvende
        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is ClubCraft.Session.Domain.Events.ParticipantJoinedEvent joinedEvent)
            {
                try
                {
                    Console.WriteLine($"[JOIN] Publishing IParticipantJoinedEvent for RoomId={joinedEvent.RoomId}");
                    await bus.Publish<ClubCraft.BuildingBlocks.Contracts.Events.IParticipantJoinedEvent>(new
                    {
                        RoomId = joinedEvent.RoomId,
                        ParticipantId = joinedEvent.ParticipantId,
                        UserId = joinedEvent.UserId,
                        ClubName = joinedEvent.ClubName
                    });
                    Console.WriteLine($"[JOIN] IParticipantJoinedEvent published successfully!");
                }
                catch (Exception pubEx)
                {
                    Console.WriteLine($"[JOIN] PUBLISH ERROR: {pubEx.GetType().Name}: {pubEx.Message}");
                    Console.WriteLine(pubEx.StackTrace);
                }
            }
        }

        return Results.Ok(new { participantId });
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"[JOIN] InvalidOperationException: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[JOIN] UNEXPECTED ERROR: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem(ex.Message);
    }
});

// GET /api/sessions/{id}/participants — HTML test sayfası için
app.MapGet("/api/sessions/{id:guid}/participants", async (Guid id, SessionDbContext dbContext) =>
{
    var gameRoom = await dbContext.GameRooms.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id);
    if (gameRoom is null) return Results.NotFound();
    return Results.Ok(gameRoom.Participants.Select(p => new { p.Id, p.UserId, p.ClubName, p.IsReady }));
});

// GET /api/sessions/{id:guid}
app.MapGet("/api/sessions/{id:guid}", async (Guid id, SessionDbContext dbContext) =>
{
    var gameRoom = await dbContext.GameRooms.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id);
    if (gameRoom is null) return Results.NotFound();
    return Results.Ok(new { gameRoom.Id, gameRoom.ShortCode, gameRoom.Status, gameRoom.CurrentWeek, gameRoom.Participants });
});

app.MapGet("/api/sessions/by-code/{shortCode}", async (string shortCode, SessionDbContext dbContext) =>
{
    var gameRoom = await dbContext.GameRooms.Include(g => g.Participants).FirstOrDefaultAsync(g => g.ShortCode == shortCode.ToUpper());
    if (gameRoom is null) return Results.NotFound();
    return Results.Ok(new { gameRoom.Id, gameRoom.ShortCode, gameRoom.Status, gameRoom.CurrentWeek });
});

app.MapPost("/api/sessions/{id:guid}/ready", async (Guid id, ReadySessionRequest request, SessionDbContext dbContext, IPublishEndpoint publishEndpoint) =>
{
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

// Geliştirme aşaması için SignalR entegrasyonu tamamlanana kadar (debug) 
// Sadece test amaçlı endpoint (WeekSimulationCompletedEvent fırlatmak için)
app.MapPost("/api/debug/publish-week-event", async (
    [Microsoft.AspNetCore.Mvc.FromBody] Guid roomId,
    MassTransit.IPublishEndpoint publishEndpoint) =>
{
    await publishEndpoint.Publish<ClubCraft.BuildingBlocks.Contracts.Events.IWeekSimulationCompletedEvent>(new
    {
        RoomId = roomId,
        CompletedWeek = 1
    });
    return Results.Ok();
});

app.Run();

public record CreateSessionRequest(string HostUserId, int MaxParticipants);
public record JoinSessionRequest(string UserId, string ClubName);
public record ReadySessionRequest(Guid ParticipantId, string Phase);
