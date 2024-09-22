using elmohandes.Server.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace elmohandes.Server.Controllers
{
	[Route("api/[controller]"),ApiController,Authorize]
	public class ProductsController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public ProductsController(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}


		[HttpGet]
		public IActionResult GetAll()
		{
			return Ok(_unitOfWork.Product.GetAll());
		}


		[HttpGet("{id:int}")]
		public IActionResult GetById(int id)
		{
			ProductDTO product = _unitOfWork.Product.GetByID(id);
			if (product is null)
				return NotFound($"not found product with id : {id} ");
			return Ok(product);
		}


		[HttpGet("GetByBrand/{BrandId:int}")]
		public IActionResult GetAllProductByBrandId(int BrandId) {
			return Ok(_unitOfWork.Product.GetAllProductByBrandId(BrandId));
		}


		[HttpGet("GetByCategory/{CategoryId:int}")]
		public IActionResult GetAllProductByCategoryId(int CategoryId)
		{
			return Ok(_unitOfWork.Product.GetAllProductByCategoryId(CategoryId));
		}


		[HttpPost,Authorize(Roles ="Admin")]
		public async Task<IActionResult> Add([FromBody] AddProductDTO NEW)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			int result = await _unitOfWork.Product.Insert(NEW);

			if (result == -1)
				return BadRequest("Enter valid categories");
			if (result == -2)
				return BadRequest("Enter valid Barnd");
			if (result == 0)
				return BadRequest("The product was not added successfully.");

			return Ok("Product added successfully.");
		}


		[HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
		public IActionResult Delete(int id)
		{
			int res = _unitOfWork.Product.Delete(id);
			if (res == 0)
				return BadRequest("The product was not Deleted");
			return Ok("Product Deleted successfully.");
		}


		[HttpPut("{id:int}"), Authorize(Roles = "Admin")]
		public async Task<IActionResult> Edit ([FromRoute]int id,[FromBody]EditProductDTO product)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			int result = await _unitOfWork.Product.Update(id,product);
			if (result == -1)
				return BadRequest("Enter valid categories");
			if (result == -2)
				return BadRequest("Enter valid Barnd");
			if (result == 0)
				return BadRequest("The product was not Edit.");

			return Ok("Product Edit successfully.");
		}

        [HttpPut("/UpdateQuantity"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateQuantity ([FromBody]UpdateQuantityDTO updateQuantity)
		{
			int res = await _unitOfWork.Product.UpdateQuantity(updateQuantity.ProductId, updateQuantity.quantity);
			if (res == -1) return BadRequest($"Not found product with this id {updateQuantity.ProductId}");
			else if (res == 0) return BadRequest($"The product was not Edit.");
			else return Ok("Done updated Quantity");
        }


    }
}

