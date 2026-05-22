using ApartmentRentalSystem.Domain.Entities;

public class Review : Entity, IAggregateRoot
{
    public int ReservationId { get; set; }
    public int AuthorId { get; set; }
    public int Rating { get; set; }          // 1–5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual Reservation Reservation { get; set; } = null!;
    public virtual User Author { get; set; } = null!;
}
