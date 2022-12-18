using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Runtime.CompilerServices;

namespace HealthCheckSample.Web.Extensions
{
    public static class Extensions
    {

        public static HealthCheckResult ToHealthCheckResult(this string healthStatus)
        {
            return healthStatus switch
            {
                "Healthy" => HealthCheckResult.Healthy(healthStatus),
                "Unhealthy" => HealthCheckResult.Unhealthy(healthStatus),
                "Degraded" => HealthCheckResult.Degraded(healthStatus),
                _ => HealthCheckResult.Unhealthy(healthStatus)
            };
        }
    }
}
