using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckSample.Web.HealthChecks
{
    public class CustomHealthCheckPublisher : IHealthCheckPublisher
    {
        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            switch (report.Status)
            {
                case HealthStatus.Unhealthy:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case HealthStatus.Degraded:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case HealthStatus.Healthy:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            Console.WriteLine($"{DateTime.UtcNow} Prob status: {report.Status}.");

            Console.ResetColor();

            return Task.CompletedTask;
        }
    }
}
