namespace elmohandes.Server.DTOs
{
    public class ChangePasswordDTO
    {
        [Required, DataType(DataType.Password)]
        public string oldPassword { get; set; }
        [Required, DataType(DataType.Password)]
        public string newPassword { get; set; }
        [Required, DataType(DataType.Password), Compare("newPassword", ErrorMessage = "Passwords do not match.")]
        public string confirmNewPassword { get; set; }
    }
}
