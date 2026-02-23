namespace ApartmentRentalSystem.Domain.Entities;
public class ApartmentPricing : Entity
{
    public int ApartmentId { get; set; }
    public int PriceTypeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;
    public virtual PriceType PriceType { get; set; } = null!;
}
