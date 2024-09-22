namespace elmohandes.Server.Models
{
	public class Order
	{
		public int Id { get; set; }
		[ForeignKey("user")]
		public string UserId { get; set; }
		public string UserName { get; set; }
		public string AddressUser { get; set; }
		[DataType(DataType.PhoneNumber)]
		public string PhoneNumberUser { get; set; }
        [DataType(DataType.EmailAddress)]
        public string EmailUser { get; set; }
        public User user { get; set; }
		public string? Notes { get; set; }

		public double ToltaPrice { get; set; }
		public DateTime OrderTime { get; set; }
		public DateTime? DeliveredTime { get; set; }
		public ICollection<OrderItems> Items { get; set; }
	}
}
