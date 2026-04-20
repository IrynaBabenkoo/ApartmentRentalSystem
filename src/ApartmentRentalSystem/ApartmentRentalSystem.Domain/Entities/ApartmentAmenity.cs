using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public class ApartmentAmenity
{
    [Display(Name = "Апартаменти")]
    public int ApartmentId { get; set; }

    [Display(Name = "Зручність")]
    public int AmenityId { get; set; }

    [Display(Name = "Апартаменти")]
    public virtual Apartment Apartment { get; set; } = null!;

    [Display(Name = "Зручність")]
    public virtual Amenity Amenity { get; set; } = null!;
}
