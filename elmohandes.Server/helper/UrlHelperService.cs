namespace elmohandes.Server.helper
{
	public class UrlHelperService : IUrlHelperService
	{
		private readonly IHttpContextAccessor _httpContextAccessor;

		public UrlHelperService(IHttpContextAccessor httpContextAccessor)
		{
			_httpContextAccessor = httpContextAccessor;
		}

		public string GetCurrentServerUrl()
		{
			var request = _httpContextAccessor.HttpContext.Request;
			var host = request.Host.Value;
			var scheme = request.Scheme;
			return $"{scheme}://{host}";
		}
	}
}
