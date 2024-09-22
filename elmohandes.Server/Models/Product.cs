namespace elmohandes.Server.Models
{
	public class Product : Base
	{
		public string Description { get; set; }
		public int Quantity { get; set; }
		public double Price { get; set; }
		public ICollection<ProductImage>? Images { get; set; }
		public ICollection<CategoryProduct> Categories { get; set; }

		[ForeignKey("Brand")]
		public int? BrandId { get; set; }
		public Brand? Brand { get; set; }
		public ICollection<CartProduct>? Products { get; set; }
	}
}
