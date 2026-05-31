using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class Apartment
{
    public int Id { get; set; }

    public int HostId { get; set; }

    public int HousingTypeId { get; set; }

    public string Title { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int MaxGuests { get; set; }

    public bool IsActive { get; set; }

    public int? UserId { get; set; }

    public string? ImagePath { get; set; }

    public decimal? Area { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<ApartmentPricing> ApartmentPricings { get; set; } = new List<ApartmentPricing>();

    public virtual User Host { get; set; } = null!;

    public virtual HousingType HousingType { get; set; } = null!;

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual User? User { get; set; }

    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
}
