﻿namespace elmohandes.Server.Data
{
	public class User : IdentityUser
	{
		[Required]
		[LettersAndSpaces]
		public string Name { get; set; }
		[ValidAddress]
		public string Address { get; set; }
		public ICollection<Order> Orders { get; set; }
	}
}