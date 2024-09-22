

namespace elmohandes.Server.DTOs
{
	public class ProductDTO : Base
	{
		public string Description { get; set; }
		public double Price { get; set; }
		public ICollection<string>? Images { get; set; } 
		public ICollection<string> NameOfCategories { get; set; }
		public int? BrandId { get; set; }
		public string BrandName { get; set; }
		public int Quantity { get; set; }
	}
}
