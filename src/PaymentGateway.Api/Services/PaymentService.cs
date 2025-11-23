using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services
{
    public class PaymentService
    {
        private readonly IAcquiringBankClient _acquiringBank;
        private readonly IPaymentRequestValidator _validator;

        public PaymentService(IAcquiringBankClient acquiringBank, IPaymentRequestValidator validator)
        {
            _acquiringBank = acquiringBank;
            _validator = validator;
        }

        public async Task<PostPaymentResponse> ProcessPaymentAsync(PostPaymentRequest paymentRequest)
        {
            var validationResult = _validator.Validate(paymentRequest);

            if (!validationResult.IsValid)
            {
                return new PostPaymentResponse
                {
                    Id = Guid.Empty,
                    Status = PaymentStatus.Rejected,
                    CardNumberLastFour = string.IsNullOrEmpty(paymentRequest.CardNumber) || paymentRequest.CardNumber.Length < 4
                                            ? paymentRequest.CardNumber
                                            : paymentRequest.CardNumber[^4..],
                    ExpiryMonth = paymentRequest.ExpiryMonth,
                    ExpiryYear = paymentRequest.ExpiryYear,
                    Amount = paymentRequest.Amount,
                    Currency = paymentRequest.Currency
                };
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CardNumber = paymentRequest.CardNumber,
                ExpiryMonth = paymentRequest.ExpiryMonth,
                ExpiryYear = paymentRequest.ExpiryYear,
                Amount = paymentRequest.Amount,
                Currency = paymentRequest.Currency,
                Cvv = paymentRequest.Cvv
            };

            var result = await _acquiringBank.PaymentAsync(payment);

            if(result.Authorized)
                payment.Status = PaymentStatus.Authorized;
            else
                payment.Status = PaymentStatus.Declined;

            payment.AuthorizationCode = result.AuthorizationCode;

            return new PostPaymentResponse
            {
                Id = payment.Id,
                Status = payment.Status,
                CardNumberLastFour = payment.CardNumber[^4..],
                ExpiryMonth = payment.ExpiryMonth,
                ExpiryYear = payment.ExpiryYear,
                Amount = payment.Amount,
                Currency = payment.Currency
            };
        }
    }
}