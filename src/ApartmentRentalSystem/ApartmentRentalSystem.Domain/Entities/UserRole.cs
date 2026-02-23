using ApartmentRentalSystem.Domain.Enums;

namespace ApartmentRentalSystem.Domain.Entities;

public class UserRole : Entity 
{
    public RoleType Type { get; set; }
    public string Name { get; set; } = string.Empty;
}