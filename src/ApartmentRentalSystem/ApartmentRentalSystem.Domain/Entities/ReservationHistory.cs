using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class ReservationHistory : Entity
{
    [Display(Name = "Бронювання")]
    public int ReservationId { get; set; }

    [Display(Name = "Хто змінив")]
    public int ChangedById { get; set; }

    [Required(ErrorMessage = "Вкажіть тип зміни")]
    [Display(Name = "Тип зміни")]
    public string ChangeType { get; set; } = string.Empty;

    [Display(Name = "Примітка/Коментар")]
    public string Note { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Дата та час зміни")]
    [DataType(DataType.DateTime)]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Бронювання")]
    public virtual Reservation Reservation { get; set; } = null!;

    [Display(Name = "Користувач")]
    public virtual User ChangedBy { get; set; } = null!;
}