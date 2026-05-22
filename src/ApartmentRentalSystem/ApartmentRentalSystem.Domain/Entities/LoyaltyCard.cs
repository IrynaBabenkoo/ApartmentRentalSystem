namespace ApartmentRentalSystem.Domain.Entities;

public class LoyaltyCard : Entity, IAggregateRoot
{
    public int UserId { get; set; }
    public int Points { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}
