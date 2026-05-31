using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class LoyaltyTransaction
{
    public int Id { get; set; }

    public int CardId { get; set; }

    public int Points { get; set; }

    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual LoyaltyCard Card { get; set; } = null!;
}
