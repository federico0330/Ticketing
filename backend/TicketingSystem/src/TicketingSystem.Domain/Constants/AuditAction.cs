namespace TicketingSystem.Domain.Constants;

public static class AuditAction
{
    public const string ReserveAttempt = "RESERVE_ATTEMPT";
    public const string ReserveSuccess = "RESERVE_SUCCESS";
    public const string ReserveFailed = "RESERVE_FAILED";
    public const string PaymentSuccess = "PAYMENT_SUCCESS";
    public const string PaymentFailed = "PAYMENT_FAILED";
    public const string ReservationExpired = "RESERVATION_EXPIRED";
}
