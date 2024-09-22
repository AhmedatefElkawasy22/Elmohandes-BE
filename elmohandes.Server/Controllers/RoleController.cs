using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace elmohandes.Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class RoleController : ControllerBase
	{
		private readonly RoleManager<IdentityRole> _roleManager;

		public RoleController(RoleManager<IdentityRole> roleManager)
		{
			_roleManager = roleManager;
		}

		
		[HttpPost("AddRole")]
		public async Task<IActionResult> AddRole(RoleDTO role)
		{
			if (ModelState.IsValid)
			{
				IdentityRole identityRole = new IdentityRole() { Name = role.RoleName };
				IdentityResult result = await _roleManager.CreateAsync(identityRole);
				if (result.Succeeded)
				{
					return Ok("Role added successfully");
				}
				else
				{
					foreach (var msg in result.Errors)
					{
						ModelState.AddModelError("", msg.Description);
					}
				}
			}
			var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
			return BadRequest(new { errors });
		}
	}
}
