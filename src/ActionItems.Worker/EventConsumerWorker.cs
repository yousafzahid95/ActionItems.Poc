using ActionItems.Worker.Events;
using ActionItems.Worker.Features.Events;
using MediatR;

namespace ActionItems.Worker;

public sealed class EventConsumerWorker : BackgroundService
{
    private static readonly Guid DemoWorkAreaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventConsumerWorker> _logger;

    public EventConsumerWorker(IServiceScopeFactory scopeFactory, ILogger<EventConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event consumer worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var demoEvent = new ActionItemStatusChangedEvent(
                DemoWorkAreaId,
                Guid.NewGuid(),
                "InProgress");

            _logger.LogInformation("Received demo event for WorkArea {WorkAreaId}", demoEvent.WorkAreaId);

            try
            {
                await mediator.Send(new ProcessActionItemStatusChangedCommand(demoEvent), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Demo event processing failed (expected when action item does not exist).");
            }
        }
    }
}
