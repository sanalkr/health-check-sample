

using HealthCheckSample.Web.HealthChecks;
using HealthCheckSample.Web.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<ApiSettingOptions>(builder.Configuration.GetSection(ApiSettingOptions.Name));

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
app.MapHealthChecks("/health");

app.Run();
