
namespace elmohandes.Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController, Authorize]
	public class UsersController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;

		public UsersController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		[HttpGet("DataUser")]
		public IActionResult GetDataUser()
		{
			DataUserDTO? currentUser = _unitOfWork.User.DataCurrentUser();
			if (currentUser == null)
			{
				return Unauthorized("User is not authenticated or an error occurred.");
			}

			return Ok(currentUser);
		}

		[HttpPut("EditDataUser")]
		public IActionResult EditDataUser(DataUserDTO data)
		{
			int result = _unitOfWork.User.EditUser(data);

			switch (result)
			{
				case > 0:
					return Ok("Data successfully edited.");
				case -1:
					return Unauthorized("User is not authenticated or an error occurred.");
				default:
					return BadRequest("Failed to edit data due to some errors.");
			}
		}

	}
}
