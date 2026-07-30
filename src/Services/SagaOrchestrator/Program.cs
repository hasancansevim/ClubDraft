using MassTransit;
using Microsoft.EntityFrameworkCore;
using ClubCraft.BuildingBlocks.Sagas;
using ClubCraft.SagaOrchestrator.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddDelayedMessageScheduler();

    x.AddSagaStateMachine<DraftPickStateMachine, DraftPickState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.AddDbContext<DbContext, DraftPickStateDbContext>((provider, builder) =>
            {
                builder.UseNpgsql("Host=127.0.0.1;Port=5439;Database=sagaorchestrator;Username=clubcraft;Password=clubcraft;");
            });
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("127.0.0.1", "/", h =>
        {
            h.Username("clubcraft");
            h.Password("clubcraft");
        });

        cfg.UseDelayedMessageScheduler();
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
