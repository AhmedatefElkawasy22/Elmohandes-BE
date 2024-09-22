namespace elmohandes.Server.DTOs
{
	public class AddOrderDTO
	{
		public string? Notes { get; set; }
		public ICollection<int> SalesBasketId { get; set; }
	}
}
