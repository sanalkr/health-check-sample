

using HealthCheckSample.Web.Extensions;
using HealthCheckSample.Web.HealthChecks;
using HealthCheckSample.Web.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<ApiSettingOptions>(builder.Configuration.GetSection(ApiSettingOptions.Name));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(ops => {
        ops.Audience = "myapp";
        //ops.Authority = "myissuer";
        ops.RequireHttpsMetadata = false;
        ops.SecurityTokenValidators.Clear();
        ops.SecurityTokenValidators.Add(new CustomTokenValidator());
        ops.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "myissuer",
            ValidAudience = "myapp",
            RequireSignedTokens = false,
            IssuerSigningKey = new SymmetricSecurityKey(new HMACSHA512(Encoding.UTF8.GetBytes("mysecretkey")).Key) { KeyId = "123" }
        };
    });

builder.Services.AddHealthChecks()
    .AddCheck<ApplicationHealthCheck>("ApplicationHealthCheck")
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    

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

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health/all")
    .RequireHost("*:7061")
    .RequireAuthorization();

app.Run();
