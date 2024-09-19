
/*
build EntityFramework


VS Code
dotnet ef dbcontext scaffold "Data Source=LAPTOP-ENCKOU6S;Initial Catalog=OnlineCodingWeb;Integrated Security=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -o Models --force

VS 2022
Scaffold-DbContext "Data Source=LAPTOP-ENCKOU6S;Initial Catalog=OnlineCodingWeb;Integrated Security=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Force



*/


using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using System.Security.Policy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddSingleton<RunProcessService>();
builder.Services.AddSingleton<EmailSendService>();
builder.Services.AddDbContext<OnlineCodingWebContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("OnlineCoding"));
});


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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


