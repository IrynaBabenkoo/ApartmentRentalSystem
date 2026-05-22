using ApartmentRentalSystem.Domain.Entities;

public class LoyaltyTransaction : Entity
{
    public int CardId { get; set; }
    public int Points { get; set; }     
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual LoyaltyCard Card { get; set; } = null!;
}
