using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class Amenity : Entity, IAggregateRoot
{
    [Required(ErrorMessage = "Назва зручності є обов'язковою")]
    [MaxLength(100, ErrorMessage = "Назва зручності не може перевищувати 100 символів")]
    [Display(Name = "Назва зручності")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Зв'язки з апартаментами")]
    public virtual ICollection<ApartmentAmenity> ApartmentAmenities { get; set; } = new List<ApartmentAmenity>();
}
