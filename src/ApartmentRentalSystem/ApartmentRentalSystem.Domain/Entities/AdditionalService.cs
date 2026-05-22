using ApartmentRentalSystem.Domain.Entities;

public class AdditionalService : Entity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public virtual ICollection<ReservationService> ReservationServices
    { get; set; } = new List<ReservationService>();
}
