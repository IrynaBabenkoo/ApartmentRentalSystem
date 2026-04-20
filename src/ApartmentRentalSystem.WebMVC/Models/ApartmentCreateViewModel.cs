using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ApartmentRentalSystem.WebMVC.Models
{
    public class ApartmentCreateViewModel
    {
        [Required(ErrorMessage = "Назва обов'язкова")]
        [Display(Name = "Назва об'єкта")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть місто")]
        [Display(Name = "Місто")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть адресу")]
        [Display(Name = "Адреса")]
        public string Address { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Від 1 до 100 гостей")]
        [Display(Name = "Макс. гостей")]
        public int MaxGuests { get; set; }

        [Required(ErrorMessage = "Оберіть тип житла")]
        [Display(Name = "Тип житла")]
        public int HousingTypeId { get; set; }

        [Required(ErrorMessage = "Вкажіть суму")]
        [Range(0.01, 1000000, ErrorMessage = "Ціна має бути більшою за 0")]
        [Display(Name = "Ціна")]
        public decimal PriceAmount { get; set; }

        [Display(Name = "Валюта")]
        public string Currency { get; set; } = "UAH";

        [Required(ErrorMessage = "Оберіть одиницю часу")]
        [Display(Name = "Період оплати")]
        public int TimeUnitId { get; set; }

        [MaxLength(2000, ErrorMessage = "Опис не може перевищувати 2000 символів")]
        [Display(Name = "Опис")]
        public string? Description { get; set; }

        [Range(1, 10000, ErrorMessage = "Площа має бути більшою за 0")]
        [Display(Name = "Площа (м²)")]
        public decimal? Area { get; set; }

        [Display(Name = "Зручності")]
        public List<int> SelectedAmenityIds { get; set; } = new();

        [Display(Name = "Фото помешкання")]
        public IFormFile? ImageFile { get; set; }
    }
}