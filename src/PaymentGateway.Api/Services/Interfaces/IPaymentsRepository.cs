using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services.Interfaces
{
    public interface IPaymentsRepository
    {
        void Add(PostPaymentResponse payment);
        PostPaymentResponse? Get(Guid id);
    }
}