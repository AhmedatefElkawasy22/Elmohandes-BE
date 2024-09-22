namespace elmohandes.Server.Sevises
{
	public class UserRepository
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _contextAccessor;

		public UserRepository(ApplicationDbContext context, IHttpContextAccessor contextAccessor)
		{
			_context = context;
			_contextAccessor = contextAccessor;
		}

		public User? GetUserByName (string Name)
	   {
			return _context.Users.AsNoTracking().SingleOrDefault(e => e.Name == Name);
	    }

		public User? GetUserByEmail(string email)
		{
			return _context.Users.AsNoTracking().SingleOrDefault(e => e.Email == email);
		}

		public DataUserDTO? DataCurrentUser()	
		{
			string? UserId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (UserId == null) 
				return null;
			User? find = _context.Users.SingleOrDefault(e => e.Id == UserId);
			if (find == null) return null;
			DataUserDTO user = new DataUserDTO
			{
				Name =        find.Name,
				Email =       find.Email,
				PhoneNumber = find.PhoneNumber,
				Address =     find.Address,
			};

			return user;
		}

		public int EditUser(DataUserDTO user) 
		{
			string? UserId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (UserId == null)
				return -1;
			User? old = _context.Users.SingleOrDefault(e => e.Id == UserId);
			if (old == null) return 0;

			try
			{
				old.Name = user.Name;
				old.PhoneNumber = user.PhoneNumber;
				old.Address = user.Address;

				_context.Users.Update(old);
				return _context.SaveChanges();
			}
			catch 
			{
				return 0;
			}

		}
	}
}
