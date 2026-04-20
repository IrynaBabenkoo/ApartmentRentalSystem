using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class TimeUnit : Entity
{
    [Required(ErrorMessage = "Вкажіть назву одиниці часу")]
    [Display(Name = "Одиниця часу (період)")]
    public string Name { get; set; } = string.Empty; // DAY, WEEK, MONTH, YEAR
}