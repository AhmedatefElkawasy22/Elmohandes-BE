
using elmohandes.Server.Models;
using System.Text.Json;

namespace elmohandes.Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class CartsController : ControllerBase
	{
		private readonly IHttpContextAccessor _contextAccessor;
		private readonly IUnitOfWork _unitOfWork;

		public CartsController(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
		{
			_unitOfWork = unitOfWork;
			_contextAccessor = contextAccessor;
		}

		[HttpPost("AddProductToCart")]
		public IActionResult AddProductToCart([FromBody]AddProductToCartDTO product)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			int res = _unitOfWork.Cart.AddProductToCart(product);

			if (res == -1)
				return Unauthorized("User is not authenticated.");
			if (res == -2)
				return BadRequest("This product was not found");
			if (res == -3)
				return BadRequest("This quantity is not available");
			if (res > 0)
				return Ok("Product added to cart successfully.");

			return BadRequest("Product Not added to cart.");
		}


		[HttpGet("GetAllProductInCart")]
		public async Task<IActionResult> GetAllProductInCart()
		{
			ICollection<ReadCartProductDTO>? Products = await _unitOfWork.Cart.GetAllProductInCartAsync();
			if (Products is null)
			{
				return BadRequest("User is not authenticated or an error occurred.");
			}
			else if (!Products.Any())
			{
				return Ok("No products in the cart yet.");
			}

			return Ok(Products);
		}


		[HttpGet("CountOfProductInCart")]
		public IActionResult CountOfProductInCart()
		{
			int Count = _unitOfWork.Cart.CountOfProductInCart();
			if (Count == -1)
			{
				return BadRequest("User is not authenticated or an error occurred.");
			}
			return Ok(Count);
		}


		[HttpPost("EditProductToCart/{ProductId:int}")]
		public IActionResult EditProductOnCart([FromRoute] int ProductId,[FromBody] EditProductToCartDTO product)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			int res = _unitOfWork.Cart.EditProductOnCart(ProductId,product);
			if (res == -1)
				return Unauthorized("User is not authenticated.");
			if (res == -2)
				return BadRequest("Not found product.");
			if (res == -3)
				return BadRequest("This quantity is not available");
			if (res > 0)
				return Ok("Edit Successfully.");

			return BadRequest("Not Edit Successfully.");

		}


		[HttpDelete("DeleteProductFromCart/{ProductId:int}")]
		public IActionResult DeleteProductFromCart(int ProductId)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);
			int res = _unitOfWork.Cart.DeleteProductFromCart(ProductId);
			if (res == -1)
				return Unauthorized("User is not authenticated.");
			if (res == -2)
				return BadRequest("Not found product.");
			if (res > 0)
				return Ok("Delete Successfully.");

			return BadRequest("Not Delete Successfully.");

		}


		[HttpDelete("DeleteAllProductFromCart")]
		public IActionResult DeleteAllProductFromCart()
		{
			int res = _unitOfWork.Cart.DeleteAllProductFromCart();

			if (res == -1)
				return Unauthorized("User is not authenticated.");
			if (res == -2)
				return BadRequest("Not found products.");
			if (res > 0)
				return Ok("Delete products Successfully.");

			return BadRequest("Not Delete products Successfully.");
		}

	}
}
