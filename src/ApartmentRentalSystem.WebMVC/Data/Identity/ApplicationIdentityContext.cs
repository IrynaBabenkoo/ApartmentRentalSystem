using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using ApartmentRentalSystem.WebMVC.Data.Identity;

namespace ApartmentRentalSystem.WebMVC.Data.Identity;

public class ApplicationIdentityContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationIdentityContext(DbContextOptions<ApplicationIdentityContext> options)
        : base(options)
    {
    }
}
