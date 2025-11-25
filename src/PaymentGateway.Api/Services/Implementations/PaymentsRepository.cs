using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Services.Implementations;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly List<PostPaymentResponse> Payments = new();

    public void Add(PostPaymentResponse payment)
    {
        Payments.Add(payment);
    }

    public PostPaymentResponse? Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}