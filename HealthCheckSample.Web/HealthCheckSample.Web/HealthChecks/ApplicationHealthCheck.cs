using HealthCheckSample.Web.Extensions;
using HealthCheckSample.Web.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HealthCheckSample.Web.HealthChecks
{
    public class ApplicationHealthCheck : IHealthCheck
    {
        private IOptions<ApiSettingOptions> _apiSettingOptions;

        public ApplicationHealthCheck(IOptions<ApiSettingOptions> apiSettingOptions)
        {
            _apiSettingOptions = apiSettingOptions;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            HealthCheckResult healthCheckResult = await CheckApiHealthAsync(context, cancellationToken);

            return healthCheckResult;
        }

        private async Task<HealthCheckResult> CheckApiHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_apiSettingOptions.Value.BaseUrl);

            using HttpResponseMessage responseMessage = await client.GetAsync(_apiSettingOptions.Value.HealthEndpoint);
            responseMessage.EnsureSuccessStatusCode();

            var responseBody = await responseMessage.Content.ReadAsStringAsync();

            return responseBody.ToHealthCheckResult();
        }
    }
}
