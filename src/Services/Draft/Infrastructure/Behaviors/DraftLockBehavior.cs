using ClubCraft.Draft.Application.Commands.ClaimPlayer;
using MediatR;
using StackExchange.Redis;

namespace ClubCraft.Draft.Infrastructure.Behaviors;

public class DraftLockBehavior : IPipelineBehavior<ClaimPlayerCommand, ClaimPlayerResult>
{
    private readonly IConnectionMultiplexer _redis;

    public DraftLockBehavior(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<ClaimPlayerResult> Handle(ClaimPlayerCommand request, RequestHandlerDelegate<ClaimPlayerResult> next, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"draft-lock:{request.DraftSessionId}";
        var lockToken = Guid.NewGuid().ToString();

        // Try to acquire lock for 5 seconds
        bool lockAcquired = await db.StringSetAsync(lockKey, lockToken, TimeSpan.FromSeconds(5), When.NotExists);

        if (!lockAcquired)
        {
            // Another claim is currently being processed for this draft session
            return ClaimPlayerResult.Fail("Az önce biri seçim yapıyordu, tekrar deneyin.");
        }

        try
        {
            return await next();
        }
        finally
        {
            // Release lock only if we own it (using a simple Lua script to guarantee atomicity)
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";
            
            await db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockToken });
        }
    }
}
