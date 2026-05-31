using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public int MethodId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public virtual PaymentMethod Method { get; set; } = null!;

    public virtual Reservation Reservation { get; set; } = null!;
}
