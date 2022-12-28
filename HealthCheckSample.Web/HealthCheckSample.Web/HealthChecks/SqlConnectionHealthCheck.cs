using HealthCheckSample.Web.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Data.Common;
using System.Data.SqlClient;

namespace HealthCheckSample.Web.HealthChecks
{
    public class SqlConnectionHealthCheck : IHealthCheck
    {
        public string _testQuery = "Select 1";
        private IOptions<ConnectionStrings> _connectionsStrings;

        public SqlConnectionHealthCheck(IOptions<ConnectionStrings> connectionStrings)
        {
            _connectionsStrings = connectionStrings;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            HealthCheckResult healthCheckResult = await CheckDBHealthAsync(context, cancellationToken);

            return healthCheckResult;
        }

        public async Task<HealthCheckResult> CheckDBHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var connection = new SqlConnection(_connectionsStrings.Value.DefaultConnection))
            {
                try
                {
                    await connection.OpenAsync(cancellationToken);

                    if (_testQuery != null)
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = _testQuery;

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch (DbException ex)
                {
                    return new HealthCheckResult(status: context.Registration.FailureStatus, exception: ex);
                }
            }

            return HealthCheckResult.Healthy();
        }
    }
}
