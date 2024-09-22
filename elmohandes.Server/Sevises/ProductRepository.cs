using elmohandes.Server.DTOs;
using elmohandes.Server.Models;
using System.Xml.Linq;

namespace elmohandes.Server.Sevises
{
	public class ProductRepository : GenricRepository<Product>
	{
		private readonly ApplicationDbContext _context;
		private readonly IUrlHelperService _urlHelperService;
		private readonly IMapper _mapper;
		public ProductRepository(ApplicationDbContext context, IMapper mapper, IUrlHelperService urlHelperService) : base(context)
		{
			_context = context;
			_mapper = mapper;
			_urlHelperService = urlHelperService;
		}
		public ICollection<ProductDTO> GetAll()
		{
			var products = _context.Products
								   .Include(p => p.Brand)
								   .Include(p => p.Categories).ThenInclude(c => c.Category)
								   .Include(p => p.Images)
								   .AsNoTracking()
								   .ToList();
			ICollection<ProductDTO> Result = _mapper.Map<ICollection<ProductDTO>>(products);

			return Result;
		}

		public ICollection<ProductDTO> GetAllProductByBrandId(int brandId)
		{
			var products = _context.Products
				                   .Where(e=>e.BrandId == brandId)
								   .Include(p => p.Brand)
								   .Include(p => p.Categories).ThenInclude(c => c.Category)
								   .Include(p => p.Images)
								   .AsNoTracking()
								   .ToList();
			ICollection<ProductDTO> Result = _mapper.Map<ICollection<ProductDTO>>(products);
			return Result;
		}


		public ICollection<ProductDTO> GetAllProductByCategoryId(int CategoryId)
		{
			var products = _context.Products
								   .Where(e => e.Categories.Any(c => c.CategoryId == CategoryId))
								   .Include(p => p.Brand)
								   .Include(p => p.Categories).ThenInclude(c => c.Category)
								   .Include(p => p.Images)
								   .AsNoTracking()
								   .ToList();
			ICollection<ProductDTO> Result = _mapper.Map<ICollection<ProductDTO>>(products);
			return Result;
		}

		public ProductDTO GetByID(int id)
		{
			Product? product = _context.Products
					.Include(p => p.Brand)
					.Include(p => p.Categories).ThenInclude(pc => pc.Category)
					.Include(p => p.Images)
					.AsNoTracking()
					.SingleOrDefault(p => p.Id == id);
			if (product == null)
				return null;

			return _mapper.Map<ProductDTO>(product);
		}

		public async Task<int> Insert(AddProductDTO entity)
		{
			Product product = _mapper.Map<Product>(entity);

			bool brandExists = await _context.Brands.AnyAsync(c => c.Id == entity.BrandId);
			if (!brandExists)
				return -2;

			if (entity.CategoriesIds != null && entity.CategoriesIds.Any())
			{
				product.Categories = new List<CategoryProduct>();
				foreach (var categoryId in entity.CategoriesIds)
				{
					// Check if the category ID is valid
					bool categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
					if (!categoryExists)
						return -1;

					product.Categories.Add(new CategoryProduct { CategoryId = categoryId });
				}
			}
			if (entity.Images != null && entity.Images.Any())
			{
				product.Images = new List<ProductImage>();
				foreach (var base64Image in entity.Images)
				{
					byte[] imageBytes = Convert.FromBase64String(base64Image);
					string fileName = $"{Guid.NewGuid()}.jpg";
					string filePath = Path.Combine("wwwroot/images", fileName);

					await File.WriteAllBytesAsync(filePath, imageBytes);

					string imageUrl = $"{_urlHelperService.GetCurrentServerUrl()}/images/{fileName}";
					product.Images.Add(new ProductImage
					{
						PathImage = imageUrl,
					});
				}
			}
			_context.Products.Add(product);
			return _context.SaveChanges();
		}

		public async Task<int> Update(int id, EditProductDTO entity)
		{
			Product? oldProduct = _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Categories).ThenInclude(pc => pc.Category)
				.Include(p => p.Images)
				.SingleOrDefault(p => p.Id == id);

			if (oldProduct != null)
			{
				// Update properties of oldProduct with values from entity
				_mapper.Map(entity, oldProduct);

				// Check if the brand ID is valid
				bool brandExists = await _context.Brands.AnyAsync(c => c.Id == entity.BrandId);
				if (!brandExists)
					return -2;

				// Update categories
				if (entity.CategoriesIds != null && entity.CategoriesIds.Any())
				{
					oldProduct.Categories.Clear();
					foreach (var categoryId in entity.CategoriesIds)
					{
						// Check if the category ID is valid
						bool categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
						if (!categoryExists)
							return -1;

						oldProduct.Categories.Add(new CategoryProduct { CategoryId = categoryId });
					}
				}


				// Update images
				if (entity.Images != null && entity.Images.Any())
				{
					// Delete old images from wwwroot/images directory
					if (oldProduct.Images != null && oldProduct.Images.Any())
					{
						foreach (var oldImage in oldProduct.Images)
						{
							string oldImagePath = Path.Combine("wwwroot/images", Path.GetFileName(oldImage.PathImage));
							if (File.Exists(oldImagePath))
							{
								File.Delete(oldImagePath);
							}
						}

						// Clear old images from the database
						oldProduct.Images.Clear();
					}

					oldProduct.Images = new List<ProductImage>();
					foreach (var base64Image in entity.Images)
					{
						byte[] imageBytes = Convert.FromBase64String(base64Image);
						string fileName = $"{Guid.NewGuid()}.jpg";
						string filePath = Path.Combine("wwwroot/images", fileName);

						await File.WriteAllBytesAsync(filePath, imageBytes);

						string imageUrl = $"{_urlHelperService.GetCurrentServerUrl()}/images/{fileName}";
						oldProduct.Images.Add(new ProductImage
						{
							PathImage = imageUrl,
						});
					}
				}

				_context.Entry(oldProduct).State = EntityState.Modified;

				return await _context.SaveChangesAsync();
			}

			return 0;
		}

		public async Task<int> UpdateQuantity (int id , int quantity)
		{
			try
			{
				Product? product = await _context.Products.SingleOrDefaultAsync(e => e.Id == id);
				if (product == null) return -1;

				product.Quantity = quantity;

				return await _context.SaveChangesAsync();
			}
			catch (Exception ex) {
				return 0;
			}
		}


        public int Delete(int id)
		{
			Product? entity = _context.Products
				.Include(p => p.Images)
				.SingleOrDefault(p => p.Id == id);

			if (entity != null)
			{
				// Delete images from the file system
				foreach (var image in entity.Images)
				{
					string imagePath = Path.Combine("wwwroot/images", Path.GetFileName(image.PathImage));
					if (File.Exists(imagePath))
					{
						File.Delete(imagePath);
					}
				}
                // Remove image records from the database
                _context.ProductImages.RemoveRange(entity.Images);

                // Delete from CategoryProduct
                var categoryProducts = _context.CategoryProducts.Where(e => e.ProductId == id).ToList();
                if (categoryProducts.Any())
                {
                    _context.CategoryProducts.RemoveRange(categoryProducts);
                }

                // Remove the product from the database
                _context.Products.Remove(entity);
				return _context.SaveChanges();
			}

			return 0;
		}


	}
}
