
/*
build EntityFramework


VS Code
dotnet ef dbcontext scaffold "Data Source=168.231.122.98;Initial Catalog=OnlineCodingWeb;Persist Security Info=True;User ID=sa;Password=NguyenH@u100304;Trust Server Certificate=True" Microsoft.EntityFrameworkCore.SqlServer -o Models --force

VS 2022
Scaffold-DbContext "Data Source=168.231.122.98;Initial Catalog=OnlineCodingWeb;Persist Security Info=True;User ID=sa;Password=NguyenH@u100304;Trust Server Certificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Force



*/


using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using System.Security.Policy;
using HMCodingWeb.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();


builder.Services.AddDbContext<OnlineCodingWebContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("OnlineCoding"));
});
builder.Services.AddTransient<RunProcessService>(); // Or Scoped if request-specific coordination needed
builder.Services.AddTransient<EmailSendService>(); // Or Singleton if thread-safe and stateless
builder.Services.AddScoped<MarkingService>();
builder.Services.AddScoped<UserPointService>();



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(3);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(3);
        options.SlidingExpiration = true;
    });



// Config Lowercase
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddSignalR();




var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<MarkingHub>("/markingHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


