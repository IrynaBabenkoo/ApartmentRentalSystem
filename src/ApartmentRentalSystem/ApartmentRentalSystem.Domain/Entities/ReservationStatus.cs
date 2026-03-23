using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class ReservationStatus : Entity
{
    [Required(ErrorMessage = "Назва статусу обов'язкова")]
    [Display(Name = "Статус бронювання")]
    public string Name { get; set; } = string.Empty; // Pending, Confirmed, Canceled
}
