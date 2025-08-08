using Hangfire;
using Hangfire.PostgreSql;
using JapaneseMealReservation.AppData;
using JapaneseMealReservation.DataTransferObject;
using JapaneseMealReservation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddDbContext<SqlServerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));


builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<MailService>();

//builder.Services.AddHostedService<AutoCompleteOrderService>();

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddHangfireServer();
// Register your job class
builder.Services.AddScoped<MealReminderJob>();


// Add authentication and set default scheme
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Session expires in 30 minutes
    options.SlidingExpiration = true; // Refresh cookie on activity

    // Optional: Automatically redirect to login on expiration
    options.Events.OnRedirectToLogin = context =>
    {
        var isApiRequest = context.Request.Path.StartsWithSegments("/Employee")
                       || context.Request.Path.StartsWithSegments("/Reservation");

        if (isApiRequest)
        {
            context.Response.StatusCode = 401; // Unauthorized
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("MyCookieAuth")
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // OPTIONAL: Add known proxy IPs if behind a proxy
    // options.KnownProxies.Add(IPAddress.Parse("YOUR_PROXY_IP")); 
    // or use this to allow all
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


builder.Services.AddSession();

var app = builder.Build();

//VERY IMPORTANT: This must come before any authentication or IP logic
app.UseForwardedHeaders();

app.UseHangfireDashboard("/hangfire");

// Every minute, check if it's 30 mins before any meal time
//RecurringJob.AddOrUpdate<MealReminderJob>(
//    "meal-reminder-job",
//    job => job.Run(),
//    Cron.Minutely);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();  // <-- Show detailed errors when developing
}
else
{
    app.UseExceptionHandler("/Home/Error");  // <-- Use friendly error page in production
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();


//Use authentication and authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
