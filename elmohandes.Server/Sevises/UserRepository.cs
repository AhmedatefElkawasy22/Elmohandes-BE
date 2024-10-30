using Microsoft.AspNetCore.Identity;

namespace elmohandes.Server.Sevises
{
    public class UserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailSender _emailSender;
        public UserManager<User> _userManager { get; }

        public UserRepository(ApplicationDbContext context, IHttpContextAccessor contextAccessor, IEmailSender emailSender, UserManager<User> userManager)
        {
            _context = context;
            _contextAccessor = contextAccessor;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public User? GetUserByName(string Name)
        {
            return _context.Users.AsNoTracking().SingleOrDefault(e => e.Name == Name);
        }

        public User? GetUserByEmail(string email)
        {
            return _context.Users.AsNoTracking().SingleOrDefault(e => e.Email == email);
        }

        public DataUserDTO? DataCurrentUser()
        {
            string? UserId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (UserId == null)
                return null;
            User? find = _context.Users.SingleOrDefault(e => e.Id == UserId);
            if (find == null) return null;
            DataUserDTO user = new DataUserDTO
            {
                Name = find.Name,
                Email = find.Email,
                PhoneNumber = find.PhoneNumber,
                Address = find.Address,
            };

            return user;
        }

        public int EditUser(DataUserDTO user)
        {
            string? UserId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (UserId == null)
                return -1;
            User? old = _context.Users.SingleOrDefault(e => e.Id == UserId);
            if (old == null) return 0;

            try
            {
                old.Name = user.Name;
                old.PhoneNumber = user.PhoneNumber;
                old.Address = user.Address;

                _context.Users.Update(old);
                return _context.SaveChanges();
            }
            catch
            {
                return 0;
            }

        }

        public async Task<string> ForgotPasswordAsync(string email)
        {
            // Check if user exists
            User? user = await _context.Users.FirstOrDefaultAsync(u=>u.Email==email);
            if (user == null)
                return "User not found.";


            // Create OTP (One-Time Password)
            var otp = GenerateOTP();
            user.OtpCode = otp;
            user.OtpExpiration = DateTime.UtcNow.AddMinutes(15); // OTP valid for 15 minutes
            _context.Users.Update(user);
            _context.SaveChanges();

            // Send OTP via email
            string subject = "Password Reset Request";
            string body = $"Your OTP code is: {otp}";
            await _emailSender.SendEmailAsync(email, subject, body);

            return "OTP sent to your email.";
        }

        public async Task<string> VerifyOtpAndResetPasswordAsync(string email, string otp, string newPassword)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return "User not found.";

            // Check if OTP is valid
            if (user.OtpCode is null || user.OtpCode != otp || user.OtpExpiration < DateTime.UtcNow)
                return "Invalid OTP.";

            // Change password
            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (!result.Succeeded)
            {
                return "Error reset password , try again";
            }

            // Clear OTP after successful verification
            user.OtpCode = null;
            user.OtpExpiration = null;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return "Password has been reset successfully.";
        }


        private string GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // 6-digit OTP
        }


        public async Task<string> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return "User is not authenticated. Please log in.";

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return "User not found.";

            IdentityResult result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return $"Error changing password: {errors}";
            }

            return "Password has been changed successfully.";
        }


    }
}
