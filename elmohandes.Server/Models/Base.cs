namespace elmohandes.Server.Models
{
	public class Base
	{
		public int Id { get; set; }
		[Required]
		[StringLength(100, ErrorMessage = "Name length can't be more than 100.")]
		public string Name { get; set; }
	}
}
