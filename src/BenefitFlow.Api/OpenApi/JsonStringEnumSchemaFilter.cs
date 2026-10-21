using System.Text.Json.Serialization;
using BenefitFlow.Api.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BenefitFlow.Api.OpenApi;

public sealed class JsonStringEnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Type = "string";
            schema.Format = null;
            schema.Enum = Enum.GetNames(context.Type)
                .Select(name => (IOpenApiAny)new OpenApiString(name))
                .ToList();
        }

        if (context.Type == typeof(CreateReservationRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["beneficiaryId"] = new OpenApiString("f87d30ea-8806-4e18-b292-0e03a52178dd"),
                ["benefitType"] = new OpenApiString("Consultation"),
                ["amount"] = new OpenApiDouble(25),
                ["currency"] = new OpenApiString("BRL")
            };
        }
    }
}
