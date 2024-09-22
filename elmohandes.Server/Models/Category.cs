using System.Text.Json.Serialization;

namespace elmohandes.Server.Models
{
	public class Category : Base
	{
		[System.Text.Json.Serialization.JsonIgnore]
		public ICollection<CategoryProduct> Products { get; set; } 

	}
}
