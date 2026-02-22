namespace ApartmentRentalSystem.Domain.Entities;

public class Apartment : Entity, IAggregateRoot
{
    public int HostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public bool IsActive { get; set; }
}