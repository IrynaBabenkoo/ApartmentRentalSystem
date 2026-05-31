using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class AdditionalService
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public virtual ICollection<ReservationService> ReservationServices { get; set; } = new List<ReservationService>();
}
