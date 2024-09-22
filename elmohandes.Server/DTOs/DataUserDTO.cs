namespace elmohandes.Server.DTOs
{
	public class DataUserDTO
	{
		[Required]
		[LettersAndSpaces]
		public string Name { get; set; }
		[ValidAddress]
		public string Address { get; set; }
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; }

		[DataType(DataType.PhoneNumber)]
		public string PhoneNumber { get; set; }
	}
}
