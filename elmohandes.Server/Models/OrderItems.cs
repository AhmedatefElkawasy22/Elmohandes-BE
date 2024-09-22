namespace elmohandes.Server.Models
{
	public class OrderItems
	{
		[ForeignKey("product")]
		public int ProductId { get; set; }
		[ForeignKey("order")]
		public int OrderId { get; set; }
		public Order order { get; set; }
		public Product product { get; set; }
		public int CountOfProduct { get; set; }
	}
}
