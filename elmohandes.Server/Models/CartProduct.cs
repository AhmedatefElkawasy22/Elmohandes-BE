namespace elmohandes.Server.Models
{
	public class CartProduct
	{
		[ForeignKey("Product")]
		public int ProductId { get; set; }
		[ForeignKey("Cart")]
		public int CartId { get; set; }
		public int CountProduct { get; set; }
		public Product Product { get; set; }
		public Cart Cart { get; set; }
	}
}
