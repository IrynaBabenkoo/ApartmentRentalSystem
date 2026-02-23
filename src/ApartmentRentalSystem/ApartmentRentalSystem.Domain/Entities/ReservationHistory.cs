namespace ApartmentRentalSystem.Domain.Entities;
public class ReservationHistory : Entity
{
    public int ReservationId { get; set; }
    public int ChangedById { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public virtual Reservation Reservation { get; set; } = null!;
    public virtual User ChangedBy { get; set; } = null!;
}