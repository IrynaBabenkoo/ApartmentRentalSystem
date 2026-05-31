using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class TimeUnit
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<PriceType> PriceTypes { get; set; } = new List<PriceType>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
