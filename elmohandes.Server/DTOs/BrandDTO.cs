namespace elmohandes.Server.DTOs
{
	public class BrandDTO
	{
		[MaxLength(100)]
		public string Name { get; set; }
	}
}
