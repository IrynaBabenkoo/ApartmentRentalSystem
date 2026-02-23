namespace ApartmentRentalSystem.Domain.Entities;

public class User : Entity, IAggregateRoot
{
    public required string FullName { get; set; } 
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int RoleId { get; set; } = 1;

    public virtual UserRole Role { get; set; } = null!;
    public virtual ICollection<Apartment> OwnedApartments { get; set; } = new List<Apartment>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}