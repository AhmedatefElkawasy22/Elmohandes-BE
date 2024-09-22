namespace elmohandes.Server.Models
{
	public class Cart
	{
		public int Id { get; set; }
		[ForeignKey("User")]
		public string UserId { get; set; }
		public User User { get; set; }
	    public ICollection<CartProduct> Products { get; set; }
	}
}
