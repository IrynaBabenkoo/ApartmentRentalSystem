using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class PriceType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int UnitId { get; set; }

    public virtual ICollection<ApartmentPricing> ApartmentPricings { get; set; } = new List<ApartmentPricing>();

    public virtual TimeUnit Unit { get; set; } = null!;
}
