using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class ReservationService
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public int ServiceId { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;

    public virtual AdditionalService Service { get; set; } = null!;
}
