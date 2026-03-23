using Infrastructure.Documentation.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;

namespace Infrastructure.Documentation;

public static class AddConfiguredSwagger
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration config)
    {
        services.AddEndpointsApiExplorer();
        var settings = config.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>();
        if (settings.Enable)
        {
            services.AddSwaggerGen(cnf =>
            {
                //version
                var Contact = new OpenApiContact()
                {
                    Name = settings.Contact,
                    Email = settings.ContactEmail
                };

                cnf.SwaggerDoc(settings.Version1, new OpenApiInfo
                {
                    Version = settings.Version1,
                    Title = settings.Title + " " + settings.Version1,
                    Description = settings.Description,
                    Contact = Contact

                });

                cnf.SwaggerDoc(settings.Version2, new OpenApiInfo
                {
                    Version = settings.Version2,
                    Title = settings.Title + " " + settings.Version2,
                    Description = settings.Description,
                    Contact = Contact
                });

                //JWT Security
                cnf.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "JWT Authentication",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                cnf.OperationFilter<SwaggerSecurityRequirement>(); // 👈 Importante no mostrar el candado en métodos con AllowAnonymous

                cnf.AddSecurityRequirement(new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "Bearer"
                                    }
                                },
                                new string[]{}
                            }
                        });
            }
            );
        }

        return services;
    }

    public static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app, IConfiguration config)
    {
        if (!config.GetValue<bool>("SwaggerSettings:Enable"))
            return app;

        // Prefijo opcional del Virtual Directory
        string virtualDir = config.GetValue<string>("SwaggerSettings:VirtualDirectory") ?? string.Empty;
        if (!string.IsNullOrEmpty(virtualDir) && !virtualDir.StartsWith("/"))
            virtualDir = "/" + virtualDir;

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            // Swagger endpoints ajustados al Virtual Directory
            c.SwaggerEndpoint($"{virtualDir}/swagger/v1/swagger.json", "MigrationExecutor v1");
            c.SwaggerEndpoint($"{virtualDir}/swagger/v2/swagger.json", "MigrationExecutor v2");
            c.RoutePrefix = "swagger"; // Swagger UI en la raíz del Virtual Directory
        });

        return app;
    }
}
