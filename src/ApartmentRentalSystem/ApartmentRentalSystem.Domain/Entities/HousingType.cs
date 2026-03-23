using System.ComponentModel.DataAnnotations;
namespace ApartmentRentalSystem.Domain.Entities;
public class HousingType : Entity
{
    [Required(ErrorMessage = "Назва типу житла є обов'язковою")]
    [Display(Name = "Тип житла")]
    public string Name { get; set; } = string.Empty;
}