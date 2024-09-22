
namespace elmohandes.Server.Data
{
	public class ApplicationDbContext : IdentityDbContext< User >
	{
		public ApplicationDbContext() { }
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		public DbSet<Product> Products { get; set; }
		public DbSet<CategoryProduct> CategoryProducts { get; set; }
		public DbSet<Brand> Brands { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<ProductImage> ProductImages { get; set; }

		public DbSet<Order> Orders { get; set; }
		public DbSet<Cart> Carts { get; set; }
		public DbSet<CartProduct> CartProducts { get; set; }
		public DbSet<OrderItems> OrderItems { get; set; }
		public DbSet<User> Users { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Configure the many-to-many relationship
			builder.Entity<CategoryProduct>().HasKey(cp => new { cp.ProductId, cp.CategoryId });
			builder.Entity<OrderItems>().HasKey(cp => new { cp.ProductId, cp.OrderId });
			builder.Entity<CartProduct>().HasKey(cp => new { cp.ProductId, cp.CartId });

			builder.Entity<CategoryProduct>()
				.HasOne(cp => cp.Product)
				.WithMany(p => p.Categories)
				.HasForeignKey(cp => cp.ProductId);

			builder.Entity<CategoryProduct>()
				.HasOne(cp => cp.Category)
				.WithMany(c => c.Products)
				.HasForeignKey(cp => cp.CategoryId);

			builder.Entity<Product>().Property(a=>a.Quantity).HasDefaultValue(1);

            builder.Entity<Order>().Property(a => a.DeliveredTime).IsRequired(false);

        }
    }
}
