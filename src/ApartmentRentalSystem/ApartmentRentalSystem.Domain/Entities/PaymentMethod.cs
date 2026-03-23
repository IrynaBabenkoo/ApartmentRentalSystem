using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class PaymentMethod : Entity
{
    [Required(ErrorMessage = "Вкажіть назву способу оплати")]
    [Display(Name = "Спосіб оплати")]
    public string Name { get; set; } = string.Empty;
}