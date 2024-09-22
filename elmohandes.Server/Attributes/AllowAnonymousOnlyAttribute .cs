public class AllowAnonymousOnlyAttribute : Attribute, IAuthorizationFilter
{
	public void OnAuthorization(AuthorizationFilterContext context)
	{
		if (context.HttpContext.User.Identity.IsAuthenticated)
		{
			context.Result = new ForbidResult();
		}
	}
}
