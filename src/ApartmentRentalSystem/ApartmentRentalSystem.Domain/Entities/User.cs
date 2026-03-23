using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class User : Entity, IAggregateRoot
{
    [Required(ErrorMessage = "Введіть повне ім'я")]
    [Display(Name = "Повне ім'я (ПІБ)")]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Некоректний формат електронної пошти")]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Некоректний номер телефону")]
    [Display(Name = "Номер телефону")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Роль у системі")]
    public int RoleId { get; set; } = 1;

    [Display(Name = "Роль користувача")]
    public virtual UserRole Role { get; set; } = null!;

    public virtual ICollection<Apartment> OwnedApartments { get; set; } = new List<Apartment>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}