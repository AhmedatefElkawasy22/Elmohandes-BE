

namespace elmohandes.Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class BrandsController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;

		public BrandsController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		[HttpGet]
		public IActionResult GetAll() {
			return Ok(_unitOfWork.Brand.GetAll());
		}


		[HttpGet("{id:int}")]
		public IActionResult GetById(int id)
		{
			Brand brand = _unitOfWork.Brand.GetByID(id);
			if (brand == null) 
				return NotFound($"Not Found Brand with id : {id} ");
			return Ok(brand);
		}


		[HttpDelete("{id:int}")]
		[Authorize(Roles = "Admin")]
		public IActionResult Delete(int id)
		{
			int res = _unitOfWork.Brand.Delete(id);
			if (res == 0)
				return BadRequest("The Brand was not Deleted");
			return Ok("Bernd Deleted successfully.");
		}


		[HttpPost,Authorize(Roles = "Admin")]
		public IActionResult Add([FromBody]BrandDTO newBrand) 
		{
			if (newBrand is null)
			{
				return BadRequest("Brand name cannot be empty.");
			}
			Brand brand = new Brand() { Name = newBrand.Name };
			int res = _unitOfWork.Brand.Insert(brand);
			if (res == 0)
				return BadRequest("The Brand was not Added ");
			return Ok("Brand Added successfully.");
		}


		[HttpPut("{id:int}"),Authorize(Roles = "Admin")]
		public IActionResult Edit([FromRoute]int id , [FromBody] BrandDTO newData)
		{
			if (newData is null)
			{
				return BadRequest("Brand name cannot be empty.");
			}
			Brand brand = new Brand() { Id = id ,Name = newData.Name };
			int res = _unitOfWork.Brand.Update(id, brand);
			if (res == 0)
				return BadRequest("The Brand was not Edit ");
			return Ok("Brand Edit successfully.");
		} 


	}
}
