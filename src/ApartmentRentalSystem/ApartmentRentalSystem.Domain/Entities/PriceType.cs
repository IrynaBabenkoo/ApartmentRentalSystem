using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class PriceType : Entity
{
    [Required(ErrorMessage = "Назва типу ціни є обов'язковою")]
    [Display(Name = "Назва тарифу")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Одиниця виміру")]
    public int UnitId { get; set; }

    [Display(Name = "Одиниця часу")]
    public virtual TimeUnit TimeUnit { get; set; } = null!;
}
