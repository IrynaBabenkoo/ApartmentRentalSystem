using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class Reservation : Entity, IAggregateRoot
{
    [Display(Name = "Апартаменти")]
    public int ApartmentId { get; set; }

    [Display(Name = "Клієнт (Гість)")]
    public int GuestId { get; set; }

    [Display(Name = "Статус")]
    public int StatusId { get; set; }

    [Display(Name = "Одиниця часу")]
    public int UnitId { get; set; }

    [Required(ErrorMessage = "Вкажіть кількість одиниць часу")]
    [Range(1, 365, ErrorMessage = "Кількість має бути від 1 до 365")]
    [Display(Name = "Кількість (днів/год)")]
    public int UnitsCount { get; set; }

    [Required(ErrorMessage = "Вкажіть дату заїзду")]
    [Display(Name = "Дата заїзду")]
    [DataType(DataType.Date)]
    public DateTime StartAt { get; set; }

    [Required(ErrorMessage = "Вкажіть дату виїзду")]
    [Display(Name = "Дата виїзду")]
    [DataType(DataType.Date)]
    public DateTime EndAt { get; set; }

    // Снапшоти (дані на момент бронювання)
    [Display(Name = "Тип ціни (архів)")]
    public int? PriceTypeIdSnapshot { get; set; }

    [Display(Name = "Ціна за одиницю (архів)")]
    public decimal? UnitAmountSnapshot { get; set; }

    [Display(Name = "Валюта (архів)")]
    public string? CurrencySnapshot { get; set; }

    [Display(Name = "Загальна вартість")]
    public decimal? TotalPrice { get; set; }

    [Display(Name = "Апартаменти")]
    public virtual Apartment Apartment { get; set; } = null!;

    [Display(Name = "Гість")]
    public virtual User Guest { get; set; } = null!;

    [Display(Name = "Поточний статус")]
    public virtual ReservationStatus Status { get; set; } = null!;

    [Display(Name = "Одиниця виміру")]
    public virtual TimeUnit TimeUnit { get; set; } = null!;

    [Display(Name = "Оплата")]
    public virtual Payment? Payment { get; set; }

    public virtual ICollection<ReservationHistory> Histories { get; set; } = new List<ReservationHistory>();
}