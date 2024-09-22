using elmohandes.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace elmohandes.Server.Controllers
{
    [Route("api/[controller]"),ApiController, Authorize ]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("/ConfirmOrder")]
        public async Task<IActionResult> ConfirmOrder(ConfirmOrderDTO order) 
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                string res = await _unitOfWork.Order.ConfirmOrderAsync(order.Notes);
                return (res == "Order has been confirmed successfully , check your email for details" ? Ok(res) : BadRequest(res));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/CancelOrder{OrderId:int}")]
        public async Task<IActionResult> CancelOrder([FromRoute] int OrderId)
        {
            try
            {
                string res = await _unitOfWork.Order.CancelOrderAsync(OrderId);
                return (res == "Order has been canceled successfully , check your email for details" ? Ok(res) : BadRequest(res));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/DeliveredOrder{OrderId:int}")]
        public async Task<IActionResult> DeliveredOrder([FromRoute] int OrderId)
        {
            try
            {
                string res = await _unitOfWork.Order.DeliveredOrderAsync(OrderId);
                return (res == "Order has been marked as delivered successfully." ? Ok(res) : BadRequest(res));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/GetOrderById{OrderId:int}")]
        public async Task<IActionResult> GetOrderById ([FromRoute] int OrderId)
        {
            OrderDTO? order = await _unitOfWork.Order.GetOrderByIdAsync(OrderId);
            if (order == null) return BadRequest($"Not found Order with id {OrderId}");
            return Ok(order);
        }
        

    }
}
