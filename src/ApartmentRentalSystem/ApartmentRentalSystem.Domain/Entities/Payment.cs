using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class Payment : Entity
{
    [Display(Name = "Бронювання")]
    public int ReservationId { get; set; }

    [Display(Name = "Спосіб оплати")]
    public int MethodId { get; set; }

    [Required(ErrorMessage = "Сума платежу обов'язкова")]
    [Range(0.01, 1000000, ErrorMessage = "Сума має бути більшою за нуль")]
    [Display(Name = "Сума до сплати")]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Валюта")]
    public string Currency { get; set; } = "USD";

    [Required]
    [Display(Name = "Статус платежу")]
    public string PaymentStatus { get; set; } = "PENDING";

    [Display(Name = "Дата та час оплати")]
    [DataType(DataType.DateTime)]
    public DateTime? PaidAt { get; set; }

    // Навігаційні властивості
    [Display(Name = "Бронювання")]
    public virtual Reservation Reservation { get; set; } = null!;

    [Display(Name = "Спосіб оплати")]
    public virtual PaymentMethod Method { get; set; } = null!;
}
