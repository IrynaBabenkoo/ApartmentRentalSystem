using ApartmentRentalSystem.Domain.Entities;

public class ReservationService : Entity
{
    public int ReservationId { get; set; }
    public int ServiceId { get; set; }
    public virtual Reservation Reservation { get; set; } = null!;
    public virtual AdditionalService Service { get; set; } = null!;
}