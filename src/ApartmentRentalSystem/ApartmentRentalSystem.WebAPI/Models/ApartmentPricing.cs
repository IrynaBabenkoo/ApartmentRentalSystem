using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class ApartmentPricing
{
    public int Id { get; set; }

    public int ApartmentId { get; set; }

    public int PriceTypeId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual PriceType PriceType { get; set; } = null!;
}
