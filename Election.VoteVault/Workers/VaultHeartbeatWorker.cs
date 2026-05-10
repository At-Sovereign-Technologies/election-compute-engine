using Election.VoteVault.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Election.VoteVault.Workers;

public class VaultHeartbeatWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public VaultHeartbeatWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    )
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();

            var sealService = scope.ServiceProvider
                .GetRequiredService<SealService>();

            var seal = sealService.GenerateSeal();

            Console.WriteLine("=== HEARTBEAT CRIPTOGRÁFICO ===");
            Console.WriteLine($"Timestamp: {seal.CreatedAt}");
            Console.WriteLine($"RootHash: {seal.RootHash}");
            Console.WriteLine($"Votes: {seal.TotalVotes}");

            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken
            );
        }
    }
}