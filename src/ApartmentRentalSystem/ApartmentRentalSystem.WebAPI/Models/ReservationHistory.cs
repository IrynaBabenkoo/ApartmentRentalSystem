using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class ReservationHistory
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public int ChangedBy { get; set; }

    public string ChangeType { get; set; } = null!;

    public string Note { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual Reservation Reservation { get; set; } = null!;
}
