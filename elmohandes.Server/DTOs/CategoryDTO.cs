namespace elmohandes.Server.DTOs
{
	public class CategoryDTO
	{
		[MaxLength(100)]
		public string Name { get; set; }
	}
}
