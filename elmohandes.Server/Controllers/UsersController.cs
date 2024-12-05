
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
            if (ModelState.IsValid)
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
            return BadRequest("data is Not vaild");
        }



        [HttpPost("ForgotPassword/{Email}"), AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromRoute] string Email)
        {
            if (ModelState.IsValid)
            {
                string res = await _unitOfWork.User.ForgotPasswordAsync(Email);
                if (res == "OTP sent to your email.")
                    return Ok(res);
                else
                    return BadRequest(res);
            }
            return BadRequest("data is Not vaild");
        }


        [HttpPost("RestPassword"), AllowAnonymous]
        public async Task<IActionResult> VerifyOtpAndResetPasswordAsync([FromBody] ForgotPasswordDTO data)
        {
            if (ModelState.IsValid)
            {
                string res = await _unitOfWork.User.VerifyOtpAndResetPasswordAsync(data.Email, data.OTP, data.Password);
                if (res == "Password has been reset successfully.")
                    return Ok(res);
                else
                    return BadRequest(res);
            }
            return BadRequest("data is Not vaild");
        }



        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO data)
        {
            if (!ModelState.IsValid)
                return BadRequest("data is Not vaild");
            string res = await _unitOfWork.User.ChangePasswordAsync(data.oldPassword, data.newPassword);
            return res switch
            {
                "User is not authenticated. Please log in." => Unauthorized(res),
                "Password has been changed successfully." => Ok(res),
                _ => BadRequest(res)
            };


        }


    }
}
