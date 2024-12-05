using System.Security.Cryptography;

namespace elmohandes.Server.Sevises
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly JwtOptions _jwtOptions;
        private readonly UnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public AuthRepository(UserManager<User> userManager, helper.JwtOptions jwtOptions, UnitOfWork unitOfWork, IEmailSender emailSender, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _jwtOptions = jwtOptions;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
        }



        public async Task<ResponseModel> RegisterAsync(RegisterUserDTO data)
        {
            ResponseModel response = new ResponseModel();
            if (await _userManager.Users.SingleOrDefaultAsync(u=>u.Name == data.Name) is not null ||await _userManager.FindByEmailAsync(data.Email) is not null)
            {
                response.Message = "This name or email is not available";
                response.StatusCode = 400;
                return response;
            }

            var identityUser = new User
            {
                UserName = $"{data.Email.Split("@")[0]}{data.PhoneNumber}",
                Name = data.Name,
                Email = data.Email,
                Address = data.Address,
                PhoneNumber = data.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(identityUser, data.Password);

            if (!result.Succeeded)
            {
                var errors = string.Empty;

                foreach (var error in result.Errors)
                    errors += $"{error.Description},";

                response.Message = errors;
                response.StatusCode = 400;
                return response;
            }


            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            token = WebUtility.UrlEncode(token);
            // Create confirmation link 
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            var confirmationLink = $"{baseUrl}/ConfirmEmail?userId={identityUser.Id}&token={token}";

            // Email body with confirmation link
            bool isArabic = data.Name.Any(c => c >= 0x0600 && c <= 0x06FF);
            string body = (isArabic ? $@"
<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; direction: rtl; text-align: right;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #fff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <h2 style='color: #333; text-align: center; font-size: 24px;'>مرحباً بك في شركة المهندس 😊</h2>
        <p style='font-size: 18px; color: #555;'>مرحباً {data.Name},</p>
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
        <p style='font-size: 18px; color: #555;'>Hello {data.Name},</p>
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

            response.Message = "registration successfully , Please check your email to confirm your account.";
            response.StatusCode = 200;
            return response;

        }

        public async Task<ResponseModel> RegistrationAsAdminAsync(RegisterUserDTO data)
        {
            ResponseModel response = new ResponseModel();
            if (_unitOfWork.User.GetUserByName(data.Name) is not null || _userManager.FindByEmailAsync(data.Email) is not null)
            {
                response.Message = "This name or email is not available";
                response.StatusCode = 400;
                return response;
            }

            var identityUser = new User
            {
                UserName = $"{data.Email.Split("@")[0]}{data.PhoneNumber}",
                Name = data.Name,
                Email = data.Email,
                Address = data.Address,
                PhoneNumber = data.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(identityUser, data.Password);

            if (!result.Succeeded)
            {
                var errors = string.Empty;

                foreach (var error in result.Errors)
                    errors += $"{error.Description},";

                response.Message = errors;
                response.StatusCode = 400;
                return response;
            }

            //Add Admin
            IdentityResult roleResult = await _userManager.AddToRoleAsync(identityUser, "Admin");
            if (!roleResult.Succeeded)
            {
                // delete the user if adding to role failed
                await _userManager.DeleteAsync(identityUser);
                var errors = string.Empty;
                foreach (var error in roleResult.Errors)
                    errors += $"{error.Description},";

                response.Message = errors;
                response.StatusCode = 400;
                return response;
            }


            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            token = WebUtility.UrlEncode(token);
            // Create confirmation link 
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            var confirmationLink = $"{baseUrl}/ConfirmEmail?userId={identityUser.Id}&token={token}";

            // Email body with confirmation link
            bool isArabic = data.Name.Any(c => c >= 0x0600 && c <= 0x06FF);
            string body = (isArabic ? $@"
<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; direction: rtl; text-align: right;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #fff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <h2 style='color: #333; text-align: center; font-size: 24px;'>مرحباً بك في شركة المهندس 😊</h2>
        <p style='font-size: 18px; color: #555;'>مرحباً {data.Name},</p>
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
        <p style='font-size: 18px; color: #555;'>Hello {data.Name},</p>
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

            response.Message = "Admin created successfully , Please check your email to confirm your account.";
            response.StatusCode = 200;
            return response;

        }

        public async Task<ResponseModel> ConfirmEmailAsync(string userId, string token)
        {
            ResponseModel response = new ResponseModel();
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                response.Message = "Invalid email confirmation request.";
                response.StatusCode = 400;
                return response;
            }


            token = WebUtility.UrlDecode(token); // Decode the token

            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Message = "User not found.";
                response.StatusCode = 400;
                return response;
            }

            try
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    user.EmailConfirmed = true;
                    response.Message = "Email confirmed successfully. Your account is now activated.";
                    response.StatusCode = 200;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.StatusCode = 400;
                return response;
            }

            response.Message = "Email confirmation failed.";
            response.StatusCode = 400;
            return response;
        }

        public async Task<AuthModel> LoginAsync(LoginUserDTo data)
        {
            var authModel = new AuthModel();
            var user = await _userManager.FindByEmailAsync(data.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, data.Password))
            {
                authModel.Message = "Email or password is Incorrect.";
                return authModel;
            }

            if (!user.EmailConfirmed)
            {
                authModel.Message = "please confirm your Email before Login.";
                return authModel;
            }

            var jwtSecurityToken = await CreateJwtTokenAsync(user);
            var rolesList = await _userManager.GetRolesAsync(user);

            authModel.IsAuthenticated = true;
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            authModel.Email = user.Email;
            authModel.ExpiresOn = jwtSecurityToken.ValidTo;
            authModel.Roles = rolesList.ToList();


            // check if user have any refresh tokens
            if (user.RefreshTokens.Any(t => t.IsActive))
            {
                var activeRefreshToken = user.RefreshTokens.FirstOrDefault(t => t.IsActive);
                authModel.RefreshToken = activeRefreshToken.Token;
                authModel.RefreshTokenExpiration = activeRefreshToken.ExpiresOn;
            }
            else
            {
                var newRefreshToken = GenerateRefreshToken();
                authModel.RefreshToken = newRefreshToken.Token;
                authModel.RefreshTokenExpiration = newRefreshToken.ExpiresOn;
                user.RefreshTokens.Add(newRefreshToken);
                await _userManager.UpdateAsync(user);
            }

            return authModel;

        }

        public async Task<AuthModel> RefreshTokenAsync(string token)
        {
            AuthModel authModel = new AuthModel();

            User? user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));
            if (user is null)
            {
                authModel.Message = "Invalid Token";
                return authModel;
            }

            RefreshToken refreshToken = user.RefreshTokens.Single(t => t.Token == token);

            if (!refreshToken.IsActive)
            {
                authModel.Message = "Inactive Token";
                return authModel;
            }
            //revoke the old Refreshtoken
            refreshToken.RevokedOn = DateTime.UtcNow;
            //Generate new Refreshtoken
            RefreshToken newRefreshtoken = GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshtoken);
            await _userManager.UpdateAsync(user);
            //Generate JwtToken Returns with request
            var newToken = await CreateJwtTokenAsync(user);

            authModel.Message = "process was successful.";
            authModel.IsAuthenticated = true;
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(newToken);
            authModel.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            authModel.RefreshToken = newRefreshtoken.Token;
            authModel.RefreshTokenExpiration = newRefreshtoken.ExpiresOn; 

            return authModel;
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {

            User? user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));
            if (user is null)
                return false;

            RefreshToken refreshToken = user.RefreshTokens.Single(t => t.Token == token);

            if (!refreshToken.IsActive)
                return false;

            //RevokeToken
            refreshToken.RevokedOn = DateTime.UtcNow;
           
            return true;
        }

        private async Task<JwtSecurityToken> CreateJwtTokenAsync(User user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();

            foreach (var role in roles)
                roleClaims.Add(new Claim(ClaimTypes.Role, role));

            var claims = new[]
            {
                new Claim(ClaimTypes.Name,user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            }.Union(userClaims).Union(roleClaims);


            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.LifeTime),
                signingCredentials: signingCredentials
                );

            return jwtSecurityToken;
        }

        private RefreshToken GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using var generator = new RNGCryptoServiceProvider();

            generator.GetBytes(randomNumber);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                ExpiresOn = DateTime.UtcNow.AddDays(2),
                CreatedOn = DateTime.UtcNow
            };
        }


    }
}
