using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public int RoleId { get; set; }

    public string Password { get; set; } = null!;

    public virtual ICollection<Apartment> ApartmentHosts { get; set; } = new List<Apartment>();

    public virtual ICollection<Apartment> ApartmentUsers { get; set; } = new List<Apartment>();

    public virtual ICollection<LoyaltyCard> LoyaltyCards { get; set; } = new List<LoyaltyCard>();

    public virtual ICollection<ReservationHistory> ReservationHistories { get; set; } = new List<ReservationHistory>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual UserRole Role { get; set; } = null!;
}
