namespace HealthCheckSample.Web.Options
{
    public class ApiSettingOptions
    {
        public static string Name = "ApiSettings";
        public string BaseUrl { get; set; }
        public string HealthEndpoint { get; set; }
    }
}
