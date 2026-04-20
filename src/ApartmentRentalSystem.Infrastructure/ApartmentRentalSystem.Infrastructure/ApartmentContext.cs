using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Domain.Enums;

namespace ApartmentRentalSystem.Infrastructure;

public class ApartmentContext : DbContext
{
    public ApartmentContext(DbContextOptions<ApartmentContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<HousingType> HousingTypes { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationStatus> ReservationStatuses { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<ApartmentPricing> ApartmentPricings { get; set; }
    public DbSet<PriceType> PriceTypes { get; set; }
    public DbSet<TimeUnit> TimeUnits { get; set; }
    public DbSet<ReservationHistory> ReservationHistories { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<Amenity> Amenities { get; set; }
    public DbSet<ApartmentAmenity> ApartmentAmenities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUser>().ToTable("AspNetUsers");

        modelBuilder.Entity<PriceType>(e =>
        {
            e.Property(x => x.UnitId).HasColumnName("unit_id");

            e.HasOne(pt => pt.TimeUnit)
                .WithMany()
                .HasForeignKey(pt => pt.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(x => x.UnitId).HasColumnName("unit_id");

            e.HasOne(r => r.TimeUnit)
                .WithMany()
                .HasForeignKey(r => r.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.PriceTypeIdSnapshot).HasColumnName("price_type_id_snapshot");
            e.Property(x => x.UnitAmountSnapshot).HasColumnName("unit_amount_snapshot").HasColumnType("decimal(10,2)");
            e.Property(x => x.CurrencySnapshot).HasColumnName("currency_snapshot").HasMaxLength(10);
            e.Property(x => x.TotalPrice).HasColumnName("total_price").HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasIndex(p => p.ReservationId).IsUnique();

            e.HasOne(p => p.Reservation)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReservationHistory>(e =>
        {
            e.Property(x => x.ChangedById).HasColumnName("changed_by");

            e.HasOne(rh => rh.ChangedBy)
                .WithMany()
                .HasForeignKey(rh => rh.ChangedById);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => x.Id);

            e.HasData(
                new UserRole { Id = 1, Type = RoleType.Guest, Name = "Guest" },
                new UserRole { Id = 2, Type = RoleType.Host, Name = "Host" }
            );
        });

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApartmentAmenity>(e =>
        {
            e.HasKey(x => new { x.ApartmentId, x.AmenityId });

            e.HasOne(x => x.Apartment)
                .WithMany(a => a.ApartmentAmenities)
                .HasForeignKey(x => x.ApartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Amenity)
                .WithMany(a => a.ApartmentAmenities)
                .HasForeignKey(x => x.AmenityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Amenity>(e =>
        {
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            e.HasData(
                new Amenity { Id = 1, Name = "Wi-Fi" },
                new Amenity { Id = 2, Name = "Парковка" },
                new Amenity { Id = 3, Name = "Кухня" },
                new Amenity { Id = 4, Name = "Кондиціонер" },
                new Amenity { Id = 5, Name = "Пральна машина" },
                new Amenity { Id = 6, Name = "Балкон" },
                new Amenity { Id = 7, Name = "Телевізор" },
                new Amenity { Id = 8, Name = "Дозволено з тваринами" }
            );
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApartmentContext).Assembly);
    }
}