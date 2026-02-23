namespace ApartmentRentalSystem.Domain.Entities;
public class PriceType : Entity
{
    public string Name { get; set; } = string.Empty;
    public int UnitId { get; set; }

    public virtual TimeUnit TimeUnit { get; set; } = null!;
}
