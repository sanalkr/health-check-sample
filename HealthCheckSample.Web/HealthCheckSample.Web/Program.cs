

using Azure.Identity;
using HealthChecks.UI.Client;
using HealthCheckSample.Web.HealthChecks;
using HealthCheckSample.Web.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<ApiSettingOptions>(builder.Configuration.GetSection(ApiSettingOptions.Name));

string containerName = builder.Configuration.GetValue<string>("StorageContainerName");
string queueName = builder.Configuration.GetValue<string>("SBQueueName");
string keyVaultUri = builder.Configuration.GetValue<string>("AzureKeyVaultUri");
string azureTag = "azure";

builder.Services.AddHealthChecks()
    .AddCheck<ApplicationHealthCheck>("ApplicationHealthCheck", tags: new string[] { "simple" })
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "Sqlserver-check", tags: new string[] { azureTag })
    .AddAzureBlobStorage(
        builder.Configuration.GetValue<string>("StorageAccountConnection"),
        containerName,
        name: "Azure Storage Blob-check",
        tags: new string[] { azureTag })
    .AddAzureServiceBusQueue(
        builder.Configuration.GetValue<string>("ServiceBusConnection"),
        queueName,
        name: "ServiceBusQueue-check",
        tags: new string[] { azureTag })
    .AddCosmosDb(
        builder.Configuration.GetConnectionString("CosmosDBConnection"),
        name: "CosmosDB-check",
        tags: new string[] { azureTag })
    .AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential(),
        ops => {
            ops.AddSecret("test-secret");
        },
        name: "AzureKeyVault-check",
        tags: new string[] { azureTag }); ;

builder.Services.AddHealthChecksUI(setting =>
{
    setting.AddHealthCheckEndpoint("Default", "/health");
})
    .AddInMemoryStorage();

builder.Services.Configure<HealthCheckPublisherOptions>(ops =>
{
    ops.Period = TimeSpan.FromSeconds(5);
    ops.Delay = TimeSpan.FromSeconds(10);
    ops.Predicate = hc => hc.Tags.Contains("simple");
});

builder.Services.AddSingleton<IHealthCheckPublisher, CustomHealthCheckPublisher>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(ops =>
    {
        ops.Audience = "myapp";
        ops.RequireHttpsMetadata = false;
        ops.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "myissuer",
            ValidAudience = "myapp",
            SignatureValidator = delegate (string token, TokenValidationParameters validationParameters)
            {
                var jwt = new JwtSecurityToken(token);
                return jwt;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
})
    .RequireHost("*:7061", "*:443");
//.RequireAuthorization();

app.MapHealthChecksUI(); //default UI - healthcheck-ui

app.MapHealthChecks("/hc", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("simple")
});

app.MapHealthChecks("/hcdetail", new HealthCheckOptions
{
    ResponseWriter = WriteResponse
});

Task WriteResponse(HttpContext context, HealthReport healthReport)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var options = new JsonWriterOptions { Indented = true };

    using var memoryStream = new MemoryStream();
    using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("status", healthReport.Status.ToString());
        jsonWriter.WriteStartObject("results");

        foreach (var entry in healthReport.Entries)
        {
            jsonWriter.WriteStartObject(entry.Key);
            jsonWriter.WriteString("status", entry.Value.Status.ToString());
            jsonWriter.WriteString("description", entry.Value.Description);
            jsonWriter.WriteStartObject("data");

            foreach (var item in entry.Value.Data)
            {
                jsonWriter.WritePropertyName(item.Key);
                JsonSerializer.Serialize(jsonWriter, item.Value,
                    item.Value?.GetType() ?? typeof(object));
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
    }

    return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
}

app.Run();
