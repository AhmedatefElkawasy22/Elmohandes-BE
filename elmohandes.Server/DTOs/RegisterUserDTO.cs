namespace elmohandes.Server.DTOs
{
	public class RegisterUserDTO
	{

		[Required,LettersAndSpaces]
		public string Name { get; set; }


		[Required,ValidAddress]
		public string Address { get; set; }


		[Required, DataType(DataType.EmailAddress)]
		public string Email { get; set; }


		[Required, DataType(DataType.PhoneNumber)]
		public string PhoneNumber { get; set; }


		[Required, DataType(DataType.Password)]
		public string Password { get; set; }


		[Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords don't match.")]
		public string ConfirmPassword { get; set; }
	}
}
