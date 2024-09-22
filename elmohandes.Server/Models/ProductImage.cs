namespace elmohandes.Server.Models
{
	public class ProductImage
	{
		public int Id { get; set; }
		public string PathImage { get; set; }
		public int ProductId { get; set; }
		public Product Product { get; set; }
	}
}
