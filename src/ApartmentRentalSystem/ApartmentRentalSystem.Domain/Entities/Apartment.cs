using System.ComponentModel.DataAnnotations; 

namespace ApartmentRentalSystem.Domain.Entities;

public class Apartment : Entity, IAggregateRoot
{
    [Display(Name = "Власник")]
    public int HostId { get; set; }

    [Display(Name = "Тип житла")]
    public int HousingTypeId { get; set; }

    [Required(ErrorMessage = "Назва апартаментів є обов'язковою")]
    [Display(Name = "Заголовок/Назва")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть місто")]
    [Display(Name = "Місто")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть адресу")]
    [Display(Name = "Адреса")]
    public string Address { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Кількість гостей має бути від 1 до 100")]
    [Display(Name = "Макс. гостей")]
    public int MaxGuests { get; set; }

    [Display(Name = "Доступно для оренди")]
    public bool IsActive { get; set; }

    [Display(Name = "Власник")]
    public virtual User Host { get; set; } = null!;

    [Display(Name = "Категорія")]
    public virtual HousingType HousingType { get; set; } = null!;

    public virtual ICollection<ApartmentPricing> Pricings { get; set; } = new List<ApartmentPricing>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}