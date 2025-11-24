using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentService.GetPaymentAsync(id);
        if (payment is null)
            return NotFound();

        return new OkObjectResult(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> CreatePaymentAsync(PostPaymentRequest request)
    {
        try
        {
            var result = await _paymentService.ProcessPaymentAsync(request);

            // Validation failure → 400
            if (result.Status == PaymentStatus.Rejected && result.Id == Guid.Empty)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (BankUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }
}