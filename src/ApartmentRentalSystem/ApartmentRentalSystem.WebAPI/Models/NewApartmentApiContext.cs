using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class NewApartmentApiContext : DbContext
{
    public NewApartmentApiContext()
    {
    }

    public NewApartmentApiContext(DbContextOptions<NewApartmentApiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdditionalService> AdditionalServices { get; set; }

    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<Apartment> Apartments { get; set; }

    public virtual DbSet<ApartmentPricing> ApartmentPricings { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<HousingType> HousingTypes { get; set; }

    public virtual DbSet<LoyaltyCard> LoyaltyCards { get; set; }

    public virtual DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PriceType> PriceTypes { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<ReservationHistory> ReservationHistories { get; set; }

    public virtual DbSet<ReservationService> ReservationServices { get; set; }

    public virtual DbSet<ReservationStatus> ReservationStatuses { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<TimeUnit> TimeUnits { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ApartmentRentalDb;Username=postgres;Password=babenkoroot26");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdditionalService>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Apartment>(entity =>
        {
            entity.HasIndex(e => e.HostId, "IX_Apartments_HostId");

            entity.HasIndex(e => e.HousingTypeId, "IX_Apartments_HousingTypeId");

            entity.HasIndex(e => e.UserId, "IX_Apartments_UserId");

            entity.Property(e => e.Description).HasMaxLength(2000);

            entity.HasOne(d => d.Host).WithMany(p => p.ApartmentHosts)
                .HasForeignKey(d => d.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.HousingType).WithMany(p => p.Apartments).HasForeignKey(d => d.HousingTypeId);

            entity.HasOne(d => d.User).WithMany(p => p.ApartmentUsers).HasForeignKey(d => d.UserId);

            entity.HasMany(d => d.Amenities).WithMany(p => p.Apartments)
                .UsingEntity<Dictionary<string, object>>(
                    "ApartmentAmenity",
                    r => r.HasOne<Amenity>().WithMany().HasForeignKey("AmenityId"),
                    l => l.HasOne<Apartment>().WithMany().HasForeignKey("ApartmentId"),
                    j =>
                    {
                        j.HasKey("ApartmentId", "AmenityId");
                        j.ToTable("ApartmentAmenities");
                        j.HasIndex(new[] { "AmenityId" }, "IX_ApartmentAmenities_AmenityId");
                    });
        });

        modelBuilder.Entity<ApartmentPricing>(entity =>
        {
            entity.HasIndex(e => e.ApartmentId, "IX_ApartmentPricings_ApartmentId");

            entity.HasIndex(e => e.PriceTypeId, "IX_ApartmentPricings_PriceTypeId");

            entity.Property(e => e.ValidFrom).HasColumnType("timestamp without time zone");
            entity.Property(e => e.ValidTo).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Apartment).WithMany(p => p.ApartmentPricings).HasForeignKey(d => d.ApartmentId);

            entity.HasOne(d => d.PriceType).WithMany(p => p.ApartmentPricings).HasForeignKey(d => d.PriceTypeId);
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<LoyaltyCard>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_LoyaltyCards_UserId");

            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.User).WithMany(p => p.LoyaltyCards).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.HasIndex(e => e.CardId, "IX_LoyaltyTransactions_CardId");

            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(d => d.Card).WithMany(p => p.LoyaltyTransactions).HasForeignKey(d => d.CardId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.MethodId, "IX_Payments_MethodId");

            entity.HasIndex(e => e.ReservationId, "IX_Payments_ReservationId").IsUnique();

            entity.Property(e => e.PaidAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Method).WithMany(p => p.Payments).HasForeignKey(d => d.MethodId);

            entity.HasOne(d => d.Reservation).WithOne(p => p.Payment).HasForeignKey<Payment>(d => d.ReservationId);
        });

        modelBuilder.Entity<PriceType>(entity =>
        {
            entity.HasIndex(e => e.UnitId, "IX_PriceTypes_unit_id");

            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.Unit).WithMany(p => p.PriceTypes)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(e => e.ApartmentId, "IX_Reservations_ApartmentId");

            entity.HasIndex(e => e.GuestId, "IX_Reservations_GuestId");

            entity.HasIndex(e => e.StatusId, "IX_Reservations_StatusId");

            entity.HasIndex(e => e.UnitId, "IX_Reservations_unit_id");

            entity.Property(e => e.CurrencySnapshot)
                .HasMaxLength(10)
                .HasColumnName("currency_snapshot");
            entity.Property(e => e.EndAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.PriceTypeIdSnapshot).HasColumnName("price_type_id_snapshot");
            entity.Property(e => e.StartAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.UnitAmountSnapshot)
                .HasPrecision(10, 2)
                .HasColumnName("unit_amount_snapshot");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Reservations).HasForeignKey(d => d.ApartmentId);

            entity.HasOne(d => d.Guest).WithMany(p => p.Reservations).HasForeignKey(d => d.GuestId);

            entity.HasOne(d => d.Status).WithMany(p => p.Reservations).HasForeignKey(d => d.StatusId);

            entity.HasOne(d => d.Unit).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReservationHistory>(entity =>
        {
            entity.HasIndex(e => e.ReservationId, "IX_ReservationHistories_ReservationId");

            entity.HasIndex(e => e.ChangedBy, "IX_ReservationHistories_changed_by");

            entity.Property(e => e.ChangedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.ReservationHistories).HasForeignKey(d => d.ChangedBy);

            entity.HasOne(d => d.Reservation).WithMany(p => p.ReservationHistories).HasForeignKey(d => d.ReservationId);
        });

        modelBuilder.Entity<ReservationService>(entity =>
        {
            entity.HasIndex(e => e.ReservationId, "IX_ReservationServices_ReservationId");

            entity.HasIndex(e => e.ServiceId, "IX_ReservationServices_ServiceId");

            entity.HasOne(d => d.Reservation).WithMany(p => p.ReservationServices).HasForeignKey(d => d.ReservationId);

            entity.HasOne(d => d.Service).WithMany(p => p.ReservationServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => e.AuthorId, "IX_Reviews_AuthorId");

            entity.HasIndex(e => e.ReservationId, "IX_Reviews_ReservationId");

            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Author).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Reservation).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.Property(e => e.Password).HasDefaultValueSql("''::text");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
