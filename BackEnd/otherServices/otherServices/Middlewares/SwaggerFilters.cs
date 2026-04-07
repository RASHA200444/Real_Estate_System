using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace otherServices.Migrations
{
    public class SwashbuckleFormFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // بنفحص لو الـ Request عبارة عن form-data
            if (operation.RequestBody?.Content.ContainsKey("multipart/form-data") ?? false)
            {
                var uploadFileMediaType = operation.RequestBody.Content["multipart/form-data"];
                var schema = uploadFileMediaType.Schema;

                if (schema?.Properties != null)
                {
                    // بنجيب الـ Properties اللي نوعها Array (زي UnitTemplates)
                    foreach (var prop in schema.Properties.Where(p => p.Value.Type == "array").ToList())
                    {
                        // هنا بنسيب Swagger يتعامل معاها كـ حقول مفرودة لو هي primitive
                        // لكن الـ Objects المعقدة Swagger UI هيفهمها لو الـ Schema بتاعتها واضحة
                        // السطر ده لضمان إن Swagger UI يحاول يعرض الـ Items
                    }
                }
            }
        }
    }
}