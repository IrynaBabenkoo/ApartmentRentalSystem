using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.Domain.Entities;

public abstract class Entity
{
    [Key] 
    [Display(Name = "Код (ID)")]
    public int Id { get; set; }
}