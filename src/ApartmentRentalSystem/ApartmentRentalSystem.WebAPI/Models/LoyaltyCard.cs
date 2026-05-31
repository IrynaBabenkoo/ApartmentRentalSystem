using System;
using System.Collections.Generic;

namespace ApartmentRentalSystem.WebAPI.Models;

public partial class LoyaltyCard
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Points { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

    public virtual User User { get; set; } = null!;
}
