namespace ApartmentRentalSystem.Domain.Entities;

public class User : Entity, IAggregateRoot
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "GUEST"; 
}