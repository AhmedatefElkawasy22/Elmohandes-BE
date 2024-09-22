using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace elmohandes.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly NBEPaymentService _nbePaymentService;

    public PaymentsController(NBEPaymentService nbePaymentService)
    {
        _nbePaymentService = nbePaymentService;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> Pay(decimal amount, string mobile, string email)
    {
        try
        {
            var paymentResult = await _nbePaymentService.InitiatePayment(amount, mobile, email);
            return Ok(paymentResult);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    }
}
