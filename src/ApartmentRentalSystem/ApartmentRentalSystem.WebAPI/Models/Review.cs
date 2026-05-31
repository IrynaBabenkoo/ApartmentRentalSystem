using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class Review
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public int AuthorId { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual Reservation Reservation { get; set; } = null!;
}
