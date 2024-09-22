namespace elmohandes.Server.DTOs
{
	public class ReadCartProductDTO
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int Quantity { get; set; }
		public double Price { get; set; }
		public int? BrandId { get; set; }
		public ICollection<string>? Images { get; set; }
		public ICollection<string> NameOfCategories { get; set; }
		public int CountProduct { get; set; }
		public double TotalPrice { get; set; }
	}
}
