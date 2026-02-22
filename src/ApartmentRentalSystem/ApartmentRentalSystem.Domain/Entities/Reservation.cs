namespace ApartmentRentalSystem.Domain.Entities;

public class Reservation : Entity, IAggregateRoot
{
    public int ApartmentId { get; set; }
    public int GuestId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending";
}