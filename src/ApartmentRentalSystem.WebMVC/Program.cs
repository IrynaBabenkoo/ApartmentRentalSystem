using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.WebMVC.Data.Identity;
using ApartmentRentalSystem.WebMVC.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.WebMVC.Infrastructure.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApartmentContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ApartmentContext")));

builder.Services.AddDbContext<ApplicationIdentityContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ApartmentContext")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
})
.AddEntityFrameworkStores<ApplicationIdentityContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); 

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDataPortServiceFactory<Apartment>, ApartmentDataPortServiceFactory>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        await app.InitializeRolesAsync();
        await app.InitializeDefaultUsersAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();