namespace elmohandes.Server.DTOs
{
	public class LoginUserDTo
	{
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; }
		public string Password { get; set; }
	}
}
