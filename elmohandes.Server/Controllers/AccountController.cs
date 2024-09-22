using elmohandes.Server.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Web;

namespace elmohandes.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        public UserManager<User> _userManager { get; }
        public IConfiguration _configuration { get; }

        public AccountController(UserManager<User> userManager, IConfiguration configuration, JwtOptions jwtOptions, IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _userManager = userManager;
            _configuration = configuration;
            _jwtOptions = jwtOptions;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        // Not used here
        #region Rdgister & Login Without EmailConfirmed 
        //[HttpPost("/Registration"), AllowAnonymousOnly]
        //public async Task<IActionResult> Registration(RegisterUserDTO user)
        //{
        //	if (_unitOfWork.User.GetUserByName(user.Name) is not null || _unitOfWork.User.GetUserByEmail(user.Email) is not null)
        //		return BadRequest("This name or email is not available");

        //	if (ModelState.IsValid)
        //	{
        //		var identityUser = new User
        //		{
        //			UserName =$"User{Guid.NewGuid()}",
        //			Name = user.Name,
        //			Email = user.Email,
        //			Address = user.Address,
        //			PhoneNumber = user.PhoneNumber
        //		};

        //		IdentityResult result = await _userManager.CreateAsync(identityUser, user.Password);

        //		if (result.Succeeded)
        //			return Ok("User created successfully");
        //		return BadRequest(result.Errors);

        //	}
        //	return BadRequest(ModelState);
        //}


        //[HttpPost("/Login"), AllowAnonymousOnly]
        //public async Task<IActionResult> Login(LoginUserDTo user)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        User? FindUser = await _userManager.FindByEmailAsync(user.Email);



        //        if (FindUser != null && await _userManager.CheckPasswordAsync(FindUser, user.Password))
        //        {
        //            if (!FindUser.EmailConfirmed)
        //            {
        //                return Unauthorized("You must confirm your email before logging in.");
        //            }
        //            // Add claims
        //            List<Claim> allclaims = new List<Claim>
        //            {
        //                  new Claim(ClaimTypes.Name, FindUser.Name),
        //                  new Claim(ClaimTypes.NameIdentifier, FindUser.Id),
        //                  new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        //            };

        //            var Roles = await _userManager.GetRolesAsync(FindUser);
        //            foreach (var role in Roles)
        //            {
        //                allclaims.Add(new Claim(ClaimTypes.Role, role));
        //            }

        //            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        //            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //            var token = new JwtSecurityToken
        //            (
        //                issuer: _jwtOptions.Issuer,
        //                audience: _jwtOptions.Audience,
        //                claims: allclaims,
        //                expires: DateTime.Now.AddHours(1),
        //                signingCredentials: credentials
        //            );

        //            return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token), expiration = token.ValidTo });
        //        }
        //        return BadRequest(FindUser != null ? "Incorrect username or password" : "User not found");
        //    }
        //    return BadRequest(ModelState);
        //} 
        #endregion

        // used here
        #region register & login with EmailConfirmed
        [HttpPost("/Registration"), AllowAnonymous]
        public async Task<IActionResult> Registration(RegisterUserDTO user)
        {
            if (_unitOfWork.User.GetUserByName(user.Name) is not null || _unitOfWork.User.GetUserByEmail(user.Email) is not null)
                return BadRequest("This name or email is not available");

            if (ModelState.IsValid)
            {
                var identityUser = new User
                {
                    UserName = $"{user.Email.Split("@")[0]}{user.PhoneNumber}",
                    Name = user.Name,
                    Email = user.Email,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = false
                };

                IdentityResult result = await _userManager.CreateAsync(identityUser, user.Password);

                if (result.Succeeded)
                {
                    // Generate email confirmation token
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                    token = WebUtility.UrlEncode(token);
                    // Create confirmation link 
                    // Don't forget  change to the correct link
                    var confirmationLink = $"{Request.Scheme}://{Request.Host}/ConfirmEmail?userId={identityUser.Id}&token={token}";

                    // Email body with confirmation link
                    bool isArabic = user.Name.Any(c => c >= 0x0600 && c <= 0x06FF);
                    string body = (isArabic ? $@"
<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; direction: rtl; text-align: right;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #fff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <h2 style='color: #333; text-align: center; font-size: 24px;'>مرحباً بك في شركة المهندس 😊</h2>
        <p style='font-size: 18px; color: #555;'>مرحباً {user.Name},</p>
        <p style='font-size: 18px; color: #6e17f9;'> شكراً لتسجيلك معنا. للبدء، يرجى تأكيد عنوان بريدك الإلكتروني بالنقر على الزر أدناه:</p>
        <div style='text-align: center; margin-top: 20px;'>
            <a href='{confirmationLink}' style='background-color: #4CAF50; color: white; padding: 14px 25px; text-decoration: none; border-radius: 5px; font-size: 18px;'>تأكيد البريد الإلكتروني</a>
        </div>
        <p style='font-size: 16px; color: #555;'>شكراً لك،</p>
        <p style='font-size: 16px; color: #555;'>شركة المهندس</p>
    </div>
</div>
<style>
    a:hover {{
        transform: scale(1.05);
        transition: all 0.3s ease;
        color: #ff0000;
    }}
</style>
" : $@" <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #fff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <h2 style='color: #333; text-align: center; font-size: 24px;'>Welcome to El-MOHANDES COMPANY 😊</h2>
        <p style='font-size: 18px; color: #555;'>Hello {user.Name},</p>
        <p style='font-size: 18px; color: #6e17f9'> Thank you for registering with us. To get started, please confirm your email address by clicking the button below:</p>
        <div style='text-align: center; margin-top: 20px;'>
            <a href='{confirmationLink}' style='background-color: #4CAF50; color: white; padding: 14px 25px; text-decoration: none; border-radius: 5px; font-size: 18px;'>Confirm Email</a>
        </div>
        <p style='font-size: 16px; color: #555;'>Thank you,</p>
        <p style='font-size: 16px; color: #555;'>El-MOHANDES COMPANY</p>
    </div>
</div>
<style>
    a:hover {{
        transform: scale(1.05);
        transition: all 0.3s ease;
        color: #ff0000;
    }}
</style> ");

                    // Send confirmation email
                    await _emailSender.SendEmailAsync(identityUser.Email, "Confirm your email address", body);

                    return Ok("User created successfully. Please check your email to confirm your account.");
                }

                return BadRequest(result.Errors);
            }

            return BadRequest(ModelState);
        }


        [Authorize(Roles = "Admin"), HttpPost("/RegistrationAsAdmin")]
        public async Task<IActionResult> RegistrationAsAdmin(RegisterUserDTO user)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_unitOfWork.User.GetUserByName(user.Name) is not null || _unitOfWork.User.GetUserByEmail(user.Email) is not null)
            {
                return BadRequest("This name or email is not available");
            }

            var identityUser = new User
            {
                UserName = $"{user.Email.Split("@")[0]}{user.PhoneNumber}",
                Name = user.Name,
                Email = user.Email,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = false
            };

            IdentityResult result = await _userManager.CreateAsync(identityUser, user.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            token = WebUtility.UrlEncode(token);

            // Don't forget  change to the correct link
            var confirmationLink = $"{Request.Scheme}://{Request.Host}/ConfirmEmail?userId={identityUser.Id}&token={token}";

            bool isArabic = user.Name.Any(c => c >= 0x0600 && c <= 0x06FF);
            string body = (isArabic ? $@"
<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; direction: rtl; text-align: right;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #fff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <h2 style='color: #333; text-align: center; font-size: 24px;'>مرحباً بك في شركة المهندس 😊</h2>
        <p style='font-size: 18px; color: #555;'>مرحباً {user.Name},</p>
        <p style='font-size: 18px; color: #6e17f9;'> شكراً لتسجيلك معنا. للبدء، يرجى تأكيد عنوان بريدك الإلكتروني بالنقر على الزر أدناه:</p>
        <div style='text-align: center; margin-top: 20px;'>
            <a href='{confirmationLink}' style='background-color: #4CAF50; color: white; padding: 14px 25px; text-decoration: none; border-radius: 5px; font-size: 18px;'>تأكيد البريد الإلكتروني</a>
        </div>
        <p style='font-size: 16px; color: #555;'>شكراً لك،</p>
        <p style='font-size: 16px; color: #555;'>شركة المهندس</p>
    </div>
</div>
<style>
    a:hover {{
        transform: scale(1.05);
        transition: all 0.3s ease;
        color: #ff0000;
    }}
</style>
" : $@" <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #fff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <h2 style='color: #333; text-align: center; font-size: 24px;'>Welcome to El-MOHANDES COMPANY 😊</h2>
        <p style='font-size: 18px; color: #555;'>Hello {user.Name},</p>
        <p style='font-size: 18px; color: #6e17f9'> Thank you for registering with us. To get started, please confirm your email address by clicking the button below:</p>
        <div style='text-align: center; margin-top: 20px;'>
            <a href='{confirmationLink}' style='background-color: #4CAF50; color: white; padding: 14px 25px; text-decoration: none; border-radius: 5px; font-size: 18px;'>Confirm Email</a>
        </div>
        <p style='font-size: 16px; color: #555;'>Thank you,</p>
        <p style='font-size: 16px; color: #555;'>El-MOHANDES COMPANY</p>
    </div>
</div>
<style>
    a:hover {{
        transform: scale(1.05);
        transition: all 0.3s ease;
        color: #ff0000;
    }}
</style> ");

            // Send confirmation email
            await _emailSender.SendEmailAsync(identityUser.Email, "Confirm your email address", body);

            // Try to add the user to the Admin role
            IdentityResult roleResult = await _userManager.AddToRoleAsync(identityUser, "Admin");
            if (!roleResult.Succeeded)
            {
                // Optionally delete the user if adding to role failed
                await _userManager.DeleteAsync(identityUser);
                return BadRequest(roleResult.Errors);
            }

            return Ok("Admin created successfully , Please check your email to confirm your account.");
        }


        [HttpGet("/ConfirmEmail"), AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest("Invalid email confirmation request.");

            token = WebUtility.UrlDecode(token); // Decode the token

            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest("User not found.");

            try
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    user.EmailConfirmed = true;
                    return Ok("Email confirmed successfully. Your account is now activated.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return BadRequest("Email confirmation failed.");
        }



        [HttpPost("/Login"), AllowAnonymousOnly]
        public async Task<IActionResult> Login(LoginUserDTo user)
        {
            if (ModelState.IsValid)
            {
                User? FindUser = await _userManager.FindByEmailAsync(user.Email);



                if (FindUser != null && await _userManager.CheckPasswordAsync(FindUser, user.Password))
                {
                    if (!FindUser.EmailConfirmed)
                    {
                        return Unauthorized("You must confirm your email before logging in.");
                    }
                    // Add claims
                    List<Claim> allclaims = new List<Claim>
                    {
                          new Claim(ClaimTypes.Name, FindUser.Name),
                          new Claim(ClaimTypes.NameIdentifier, FindUser.Id),
                          new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                    var Roles = await _userManager.GetRolesAsync(FindUser);
                    foreach (var role in Roles)
                    {
                        allclaims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
                    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken
                    (
                        issuer: _jwtOptions.Issuer,
                        audience: _jwtOptions.Audience,
                        claims: allclaims,
                        expires: DateTime.Now.AddHours(1),
                        signingCredentials: credentials
                    );

                    return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token), expiration = token.ValidTo });
                }
                return BadRequest(FindUser != null ? "Incorrect username or password" : "User not found");
            }
            return BadRequest(ModelState);
        } 
        #endregion



    }
}
