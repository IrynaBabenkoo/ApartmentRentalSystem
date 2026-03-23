using System.ComponentModel.DataAnnotations;
using ApartmentRentalSystem.Domain.Enums;

namespace ApartmentRentalSystem.Domain.Entities;

public class UserRole : Entity
{
    [Display(Name = "Тип ролі (Enum)")]
    public RoleType Type { get; set; }

    [Required(ErrorMessage = "Назва ролі є обов'язковою")]
    [Display(Name = "Назва ролі")]
    public string Name { get; set; } = string.Empty;
}