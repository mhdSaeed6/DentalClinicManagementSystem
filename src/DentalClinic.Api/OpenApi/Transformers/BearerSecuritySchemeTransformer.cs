using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DentalClinic.Api.OpenApi.Transformers;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string SchemeId = JwtBearerDefaults.AuthenticationScheme;

    // 1. إضافة الـ SecurityScheme للـ Document
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Bearer token",
            Name = "Authorization"
        };

        return Task.CompletedTask;
    }

    // 2. تطبيق الـ Requirement على الـ Endpoints التي تطلب Auth فقط
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        // 🟢 تحقق من وجود Authorize وعدم وجود AllowAnonymous
        var hasAuthorize = metadata.OfType<IAuthorizeData>().Any();
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();

        if (hasAuthorize && !allowsAnonymous)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();

            var securityScheme = new OpenApiSecuritySchemeReference(SchemeId, context.Document, null);

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [securityScheme] = []
            });
        }

        return Task.CompletedTask;
    }
}