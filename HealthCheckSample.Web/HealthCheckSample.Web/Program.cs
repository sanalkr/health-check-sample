

using HealthCheckSample.Web.Extensions;
using HealthCheckSample.Web.HealthChecks;
using HealthCheckSample.Web.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<ApiSettingOptions>(builder.Configuration.GetSection(ApiSettingOptions.Name));

//Adding Authentication
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(ops => {
//        ops.Audience = "myapp";
//        ops.RequireHttpsMetadata = false;
//        ops.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidIssuer = "myissuer",
//            ValidAudience = "myapp",
//            SignatureValidator = delegate (string token, TokenValidationParameters validationParameters)
//            {
//                var jwt = new JwtSecurityToken(token);
//                return jwt;
//            }
//        };
//    });

builder.Services.AddHealthChecks()
    .AddCheck<ApplicationHealthCheck>("ApplicationHealthCheck", tags: new string[] { "simple" })
    ;//.AddCheck<SqlConnectionHealthCheck>("SqlHealthCheck");
    //.AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.Configure<HealthCheckPublisherOptions>(ops =>
{
    ops.Period = TimeSpan.FromSeconds(5);
    ops.Delay = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IHealthCheckPublisher, CustomHealthCheckPublisher>();

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

app.MapHealthChecks("/health/all");
//.RequireHost("*:7061")
//.RequireAuthorization();
app.MapHealthChecks("/hc", new HealthCheckOptions {
    Predicate = hc => hc.Tags.Contains("simple")
});

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/hcdetail", new HealthCheckOptions { 
    ResponseWriter = WriteResponse
});

app.Run();

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