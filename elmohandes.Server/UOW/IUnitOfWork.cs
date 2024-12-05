namespace elmohandes.Server.UOW
{
	public interface IUnitOfWork
	{
		public ProductRepository Product { get; }
		public GenricRepository<Brand> Brand {  get; } 
		public GenricRepository<Category> Category {  get; }
		public CartRepository Cart { get; }
		public UserRepository User { get; }
		public OrderRepository Order { get; }
		public AuthRepository Auth { get; }
	}
}
