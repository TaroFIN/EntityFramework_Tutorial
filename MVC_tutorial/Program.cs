using Pelican.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Pelican.DataAccess.Repository.IRepository;
using Pelican.DataAccess.Repository;
using Pomelo.EntityFrameworkCore.MySql;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Pelican.Models;
using Pelican.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Pelican.DataAccess.DbInitializer;

var builder = WebApplication.CreateBuilder(args);

var serverVision = new MySqlServerVersion(new Version(10, 11, 6, 1369));
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
//    serverVision));

builder.Services.Configure<ECPay>(builder.Configuration.GetSection("ECPay"));
ECPay.HashKey = builder.Configuration.GetSection("ECPay:HashKey").Get<string>();
ECPay.HashIV = builder.Configuration.GetSection("ECPay:HashIV").Get<string>();

builder.Services.Configure<FacebookLoginKey>(builder.Configuration.GetSection("FacebookLoginKey"));
FacebookLoginKey.AppId = builder.Configuration.GetSection("FacebookLoginKey:AppId").Get<string>();
FacebookLoginKey.AppSecret = builder.Configuration.GetSection("FacebookLoginKey:AppSecret").Get<string>();
MicrosoftLoginKey.ClientId = builder.Configuration.GetSection("MicrosoftLoginKey:ClientId").Get<string>();
MicrosoftLoginKey.ClientSecret = builder.Configuration.GetSection("MicrosoftLoginKey:ClientSecret").Get<string>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddAuthentication().AddFacebook(option =>
{
    option.AppId = FacebookLoginKey.AppId;
    option.AppSecret = FacebookLoginKey.AppSecret;
});

builder.Services.AddAuthentication().AddMicrosoftAccount(option =>
{
    option.ClientId = MicrosoftLoginKey.ClientId;
    option.ClientSecret = MicrosoftLoginKey.ClientSecret;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
SeedDatabase();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();


void SeedDatabase()
{
    using(var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}