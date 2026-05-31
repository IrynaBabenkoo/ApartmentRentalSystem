using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class Reservation
{
    public int Id { get; set; }

    public int ApartmentId { get; set; }

    public int GuestId { get; set; }

    public int StatusId { get; set; }

    public int UnitId { get; set; }

    public int UnitsCount { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int? PriceTypeIdSnapshot { get; set; }

    public decimal? UnitAmountSnapshot { get; set; }

    public string? CurrencySnapshot { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual User Guest { get; set; } = null!;

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<ReservationHistory> ReservationHistories { get; set; } = new List<ReservationHistory>();

    public virtual ICollection<ReservationService> ReservationServices { get; set; } = new List<ReservationService>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ReservationStatus Status { get; set; } = null!;

    public virtual TimeUnit Unit { get; set; } = null!;
}
