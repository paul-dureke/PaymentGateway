using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services
{
    public class PaymentService
    {
        private readonly IAcquiringBankClient _acquiringBank;

        public PaymentService(IAcquiringBankClient acquiringBank)
        {
            _acquiringBank = acquiringBank;
        }

        public async Task<PostPaymentResponse> ProcessPaymentAsync(PostPaymentRequest paymentRequest)
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CardNumber = paymentRequest.CardNumberLastFour,
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
                CardNumberLastFour = payment.CardNumber,
                ExpiryMonth = payment.ExpiryMonth,
                ExpiryYear = payment.ExpiryYear,
                Amount = payment.Amount,
                Currency = payment.Currency
            };
        }
    }
}