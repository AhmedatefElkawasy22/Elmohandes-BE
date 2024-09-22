namespace elmohandes.Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class CategorysController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;

		public CategorysController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		[HttpGet]
		public IActionResult GetAll()
		{
			return Ok(_unitOfWork.Category.GetAll());
		}

		[HttpGet("{id:int}")]
		public IActionResult GetById(int id)
		{
			Category? Category = _unitOfWork.Category.GetByID(id);
			if (Category == null)
				return NotFound($"Not Found Category with id : {id} ");
			return Ok(Category);
		}

		[HttpDelete("{id:int}")]
		[Authorize(Roles = "Admin")]
		public IActionResult Delete(int id)
		{
			int res = _unitOfWork.Category.Delete(id);
			if (res == 0)
				return BadRequest("This category does not exist, or a problem occurred.");
			return Ok("Bernd Deleted successfully.");
		}


		[HttpPost,Authorize(Roles = "Admin")]
		public IActionResult Add([FromBody] BrandDTO NewCategory)
		{
			if (NewCategory is null)
			{
				return BadRequest("Category name cannot be empty.");
			}
			Category Category = new Category() { Name = NewCategory.Name };
			int res = _unitOfWork.Category.Insert(Category);
			if (res == 0)
				return BadRequest("The Category was not Added . ");
			return Ok("Category Added successfully.");
		}


		[HttpPut("{id:int}"),Authorize(Roles = "Admin")]
		public IActionResult Edit([FromRoute] int id, [FromBody] BrandDTO newData)
		{
			if (newData is null)
			{
				return BadRequest("Category name cannot be empty.");
			}
			Category Category = new Category() { Id = id, Name = newData.Name };
			int res = _unitOfWork.Category.Update(id, Category);
			if (res == 0)
				return BadRequest("This category does not exist, or a problem occurred. ");
			return Ok("Category Edit successfully.");
		}
	}
}
