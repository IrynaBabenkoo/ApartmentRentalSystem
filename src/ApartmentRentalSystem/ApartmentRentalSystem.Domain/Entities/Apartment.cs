namespace ApartmentRentalSystem.Domain.Entities;

public class Apartment : Entity, IAggregateRoot
{
    public int HostId { get; set; }
    public int HousingTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public bool IsActive { get; set; }

    public virtual User Host { get; set; } = null!;
    public virtual HousingType HousingType { get; set; } = null!;
    public virtual ICollection<ApartmentPricing> Pricings { get; set; } = new List<ApartmentPricing>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}