namespace elmohandes.Server.AutoMapper
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Product, ProductDTO>()
				.ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand.Name))
				.ForMember(dest => dest.BrandId, opt => opt.MapFrom(src => src.Brand.Id))
				.ForMember(dest => dest.NameOfCategories, opt => opt.MapFrom(src => src.Categories.Select(d => d.Category.Name)))
				.ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images.Select(e=>e.PathImage)))
				.ReverseMap()
				.ForMember(dest => dest.Brand, opt => opt.Ignore())
				.ForMember(dest => dest.Categories, opt => opt.Ignore());

			CreateMap<AddProductDTO, Product>()
				.ForMember(dest => dest.Categories, opt => opt.Ignore())
				.ForMember(dest => dest.Images, opt => opt.Ignore());

			CreateMap<EditProductDTO, Product>()
				.ForMember(dest => dest.Categories, opt => opt.Ignore())
				.ForMember(dest => dest.Images, opt => opt.Ignore())
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.Brand, opt => opt.Ignore());
		}
	}
}