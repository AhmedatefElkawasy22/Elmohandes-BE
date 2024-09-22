namespace elmohandes.Server.DTOs
{
    public class OrderDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string AddressUser { get; set; }
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumberUser { get; set; }
        [DataType(DataType.EmailAddress)]
        public string EmailUser { get; set; }
        public string? Notes { get; set; }
        public double ToltaPrice { get; set; }
        public DateTime OrderTime { get; set; }
        public DateTime? DeliveredTime { get; set; }
        public List<Dictionary<string,int>> Products { get; set; }
    }
}
