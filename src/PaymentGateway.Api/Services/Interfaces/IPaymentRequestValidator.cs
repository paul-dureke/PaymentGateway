using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services.Interfaces
{
    public interface IPaymentRequestValidator
    {
        PaymentRequestValidationResult Validate(PostPaymentRequest request);
    }

    public record PaymentRequestValidationResult(bool IsValid, string? ErrorMessage);
}