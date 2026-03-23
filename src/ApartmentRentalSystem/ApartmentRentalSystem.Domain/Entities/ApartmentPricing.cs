using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class ApartmentPricing : Entity
{
    [Display(Name = "Апартаменти")]
    public int ApartmentId { get; set; }

    [Display(Name = "Тип ціни")]
    public int PriceTypeId { get; set; }

    [Required(ErrorMessage = "Вкажіть суму")]
    [Range(0.01, 1000000, ErrorMessage = "Ціна повинна бути більшою за 0")]
    [Display(Name = "Сума")]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Валюта")]
    public string Currency { get; set; } = "USD";

    [Required(ErrorMessage = "Вкажіть дату початку")]
    [Display(Name = "Діє з")]
    [DataType(DataType.Date)] 
    public DateTime ValidFrom { get; set; }

    [Display(Name = "Діє до")]
    [DataType(DataType.Date)]
    public DateTime? ValidTo { get; set; }

    [Display(Name = "Апартаменти")]
    public virtual Apartment Apartment { get; set; } = null!;

    [Display(Name = "Тип ціни")]
    public virtual PriceType PriceType { get; set; } = null!;
}