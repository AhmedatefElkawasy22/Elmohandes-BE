namespace elmohandes.Server.Sevises
{
	public class CartRepository
	{

		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _contextAccessor;
		public CartRepository(ApplicationDbContext context, IHttpContextAccessor contextAccessor)
		{
			_context = context;
			_contextAccessor = contextAccessor;
		}
		public int AddProductToCart(AddProductToCartDTO product)
		{
			string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userId))
			{
				return -1;
			}

			Cart? cart = GetCartByUserId(userId);

			if (cart is null)
			{
				Cart NewCart = new Cart() { UserId = userId };
				int res = AddCart(NewCart);
				if (res == 0) return 0;
				if (res > 0)
					cart = GetCartByUserId(userId);
			}
			if (cart is null) return 0;

			CartProduct NewProduct = new CartProduct()
			{
				CartId = cart.Id,
				ProductId = product.ProductId,
			};
			Product? p = _context.Products.AsNoTracking().SingleOrDefault(p => p.Id == product.ProductId);
			if (p is null) return -2;
			if (p.Quantity < product.CountProduct)
				return -3;

			NewProduct.CountProduct = product.CountProduct;
			_context.CartProducts.Add(NewProduct);
			return _context.SaveChanges();
		}

		public Cart? GetCartByUserId(string id)
		{
			return _context.Carts.SingleOrDefault(e => e.UserId == id);
		}

		public int AddCart(Cart NewCart)
		{
			_context.Carts.Add(NewCart);
			return _context.SaveChanges();
		}

		public async Task<ICollection<ReadCartProductDTO>?> GetAllProductInCartAsync()
		{
			try
			{
				string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

				if (string.IsNullOrEmpty(userId)) // user is not authenticated				
					return null;

				Cart? cart = GetCartByUserId(userId);

				if (cart is null)
					return Array.Empty<ReadCartProductDTO>();

				ICollection<CartProduct> products = await _context.CartProducts
	               .Where(d => d.CartId == cart.Id)
	               .Include(e => e.Product)
	               	.ThenInclude(p => p.Categories)
					  .ThenInclude(c=>c.Category)
	               .Include(e => e.Product)
	               	.ThenInclude(p => p.Images) 
	               .ToListAsync();


				var res = new List<ReadCartProductDTO>();

				foreach (var product in products)
				{
					var productDTO = new ReadCartProductDTO
					{
						Id = product.Product.Id,
						Name= product.Product.Name,
						Description= product.Product.Description,
						Quantity = product.Product.Quantity,
						Price = product.Product.Price,
						BrandId = product.Product.BrandId,
						NameOfCategories = product.Product.Categories.Select(e=>e.Category.Name).ToList(),
						Images = product.Product.Images.Select(e=>e.PathImage).ToList(),
						CountProduct = product.CountProduct,
						TotalPrice = product.CountProduct * product.Product.Price
					};
					res.Add(productDTO);
				}

				return res;
			}
			catch (Exception ex)
			{
				// Log the exception
				Console.WriteLine($"An error occurred: {ex.Message}");
				return null;
			}
		}

		public int CountOfProductInCart()
		{
			string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userId)) // user is not authenticated				
				return -1;

			Cart? cart = GetCartByUserId(userId);

			if (cart is null)
				return 0;

			return _context.CartProducts
				.Where(d => d.CartId == cart.Id)
				.AsNoTracking().Count();
		}


		public int EditProductOnCart(int ProductId,EditProductToCartDTO Product)
		{
			string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userId))
				return -1;

			Cart? cart = GetCartByUserId(userId);
			if (cart is null)
				return -2;

			CartProduct? old = _context.CartProducts.AsNoTracking().SingleOrDefault(e => e.CartId == cart.Id && e.ProductId == ProductId);

			if (old is null )return -2;

			Product? p = _context.Products.AsNoTracking().SingleOrDefault(p => p.Id == ProductId);

			if (p is null) return -2;

			if (p.Quantity < Product.CountProduct)
				return -3;

			old.CountProduct = Product.CountProduct;
			_context.CartProducts.Update(old);

			return _context.SaveChanges();
		}

		public int DeleteProductFromCart(int ProductId) 
		{
			string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userId))
				return -1;

			Cart? cart = GetCartByUserId(userId);
			if (cart is null)
				return -2;

			CartProduct? old = _context.CartProducts.AsNoTracking().SingleOrDefault(e => e.CartId == cart.Id && e.ProductId == ProductId);

			if (old is null)
				return -2;

			_context.CartProducts.Remove(old);
			return _context.SaveChanges();
		}	

		public int DeleteAllProductFromCart()
		{
			string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userId))
				return -1;

			Cart? cart = GetCartByUserId(userId);
			if (cart is null)
				return -2;

			var cartProducts = _context.CartProducts.Where(cp => cp.CartId == cart.Id).ToList();

			if (cartProducts == null || !cartProducts.Any())
				return -2;

			_context.CartProducts.RemoveRange(cartProducts);

			return _context.SaveChanges();
		}
	}
}
