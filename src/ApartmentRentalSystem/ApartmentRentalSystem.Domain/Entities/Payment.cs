namespace ApartmentRentalSystem.Domain.Entities;
public class Payment : Entity
{
    public int ReservationId { get; set; }
    public int MethodId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentStatus { get; set; } = "PENDING";
    public DateTime? PaidAt { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;
    public virtual PaymentMethod Method { get; set; } = null!;
}
