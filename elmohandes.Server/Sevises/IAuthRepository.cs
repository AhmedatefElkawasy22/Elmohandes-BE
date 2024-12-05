namespace elmohandes.Server.Sevises
{
    public interface IAuthRepository
    {
        Task<ResponseModel> RegisterAsync(RegisterUserDTO data);
        Task<ResponseModel> RegistrationAsAdminAsync(RegisterUserDTO data);
        Task<ResponseModel> ConfirmEmailAsync(string userId, string token);
        Task<AuthModel> LoginAsync(LoginUserDTo data);
        Task<AuthModel> RefreshTokenAsync(string token);
        Task<bool> RevokeTokenAsync(string token);
    }
}
