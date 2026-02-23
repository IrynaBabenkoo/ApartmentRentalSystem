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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApartmentContext).Assembly);
    }
}