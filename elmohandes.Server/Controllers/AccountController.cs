using Azure;

namespace elmohandes.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
    
        public AccountController( IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        #region register & login with EmailConfirmed & RefreshToken

        [HttpPost("/Registration"), AllowAnonymous]
        public async Task<IActionResult> Registration([FromBody]RegisterUserDTO user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            ResponseModel response = await _unitOfWork.Auth.RegisterAsync(user);

            if (response == null) 
                return BadRequest("An error occurred while processing your request.");

            if (response.StatusCode==400)
                return BadRequest(response.Message);

            return Ok(response.Message);
        }


        [Authorize(Roles = "Admin"), HttpPost("/RegistrationAsAdmin")]
        public async Task<IActionResult> RegistrationAsAdmin([FromBody]RegisterUserDTO user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            ResponseModel response = await _unitOfWork.Auth.RegistrationAsAdminAsync(user);

            if (response == null)
                return BadRequest("An error occurred while processing your request.");

            if (response.StatusCode == 400)
                return BadRequest(response.Message);

            return Ok(response.Message);
        }


        [HttpGet("/ConfirmEmail"), AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            ResponseModel response = await _unitOfWork.Auth.ConfirmEmailAsync(userId, token);

            if (response == null)
                return BadRequest("An error occurred while processing your request.");

            if (response.StatusCode == 400)
                return BadRequest(response.Message);

            return Ok(response.Message);
        }


        [HttpPost("/Login"), AllowAnonymousOnly]
        public async Task<IActionResult> Login(LoginUserDTo user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            AuthModel response = await _unitOfWork.Auth.LoginAsync(user);

            if (response == null)
                return BadRequest("An error occurred while processing your request.");

            if (!response.IsAuthenticated)
                return BadRequest(response.Message);

            if (!string.IsNullOrEmpty(response.RefreshToken))
                SetRefreshTokenInCookie(response.RefreshToken,response.RefreshTokenExpiration);


            return Ok(response);

        }

        [HttpGet("/RefreshToken")]
        public async Task<IActionResult> RefreshToken()
        {
            string? refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest("Token Is Requird.");

            AuthModel response = await _unitOfWork.Auth.RefreshTokenAsync(refreshToken);

            if (!response.IsAuthenticated)
                return BadRequest(response.Message);

            SetRefreshTokenInCookie(response.RefreshToken, response.RefreshTokenExpiration);

            return Ok(response);
        }

        [HttpPost("/RevokeToken")]
        public async Task<IActionResult> RevokeToken()
        {
            string? token = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token Is Requird.");
            bool res = await _unitOfWork.Auth.RevokeTokenAsync(token);
            if (!res)
                return BadRequest("Invail token");

            return Ok("token has been revoked successfully.");
        }

        private void SetRefreshTokenInCookie(string refreshToken, DateTime expiresToken)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = expiresToken,
                HttpOnly = true
            };
            Response.Cookies.Append("RefreshToken" , refreshToken, cookieOptions);
        }
        #endregion




    }
}
