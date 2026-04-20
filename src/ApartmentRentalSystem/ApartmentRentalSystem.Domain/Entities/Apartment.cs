using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ApartmentRentalSystem.Domain.Entities;

public class Apartment : Entity, IAggregateRoot
{
    [Display(Name = "Фото помешкання")]
    public string? ImagePath { get; set; }

    [Display(Name = "Власник")]
    public string HostId { get; set; } = string.Empty;

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

    [MaxLength(2000, ErrorMessage = "Опис не може перевищувати 2000 символів")]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Range(1, 10000, ErrorMessage = "Площа має бути більшою за 0")]
    [Display(Name = "Площа (м²)")]
    public decimal? Area { get; set; }

    [Display(Name = "Доступно для оренди")]
    public bool IsActive { get; set; }

    [Display(Name = "Власник")]
    public virtual IdentityUser Host { get; set; } = null!;

    [Display(Name = "Категорія")]
    public virtual HousingType HousingType { get; set; } = null!;

    [Display(Name = "Ціни")]
    public virtual ICollection<ApartmentPricing> Pricings { get; set; } = new List<ApartmentPricing>();

    [Display(Name = "Бронювання")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    [Display(Name = "Зручності")]
    public virtual ICollection<ApartmentAmenity> ApartmentAmenities { get; set; } = new List<ApartmentAmenity>();
}