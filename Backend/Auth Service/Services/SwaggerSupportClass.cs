namespace Auth.Services
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();

            operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
        }
    }
}
