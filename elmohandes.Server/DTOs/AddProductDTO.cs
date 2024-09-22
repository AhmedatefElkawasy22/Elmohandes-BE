namespace elmohandes.Server.DTOs
{
	public class AddProductDTO
	{
		[Required]
		[StringLength(100, ErrorMessage = "Name length can't be more than 100.")]
		public string Name { get; set; }
		[Required]
		[StringLength(700, ErrorMessage = "Description length can't be more than 700.")]
		public string Description { get; set; }
		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
		public double Price { get; set; }
		public ICollection<string>? Images { get; set; }
		public ICollection<int> CategoriesIds { get; set; }
		public int? BrandId { get; set; }
		public int Quantity { get; set; }
	}
}
