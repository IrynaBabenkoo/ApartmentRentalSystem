namespace ApartmentRentalSystem.Domain.Entities;
public class ReservationStatus : Entity
{
    public string Name { get; set; } = string.Empty; // Pending, Confirmed, Canceled
}
