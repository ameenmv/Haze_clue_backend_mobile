using HazeClue.UI.StartupExtensions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace HazeClue.UI
{
    /// <summary>
    /// Entry point of the HazeClue Web API application.
    /// Responsible for configuring and launching the web host,
    /// setting up middleware, and initializing API versioning and Swagger UI.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Configures the application pipeline and starts the web server.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.ConfigureServices(builder.Configuration);

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecific", policy =>
                {
                    policy.SetIsOriginAllowed(origin => true) // Allow any origin (fixes Flutter Web random ports)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });

            builder.Services.AddSignalR();

            // Health Checks
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<HazeClue.Infrastructure.DbContext.ApplicationDbContext>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
           
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }
            });

            app.UseHsts();
            app.UseHttpsRedirection();

            app.UseCors("AllowSpecific"); // Apply CORS before routing

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.MapHub<HazeClue.UI.Hubs.SessionHub>("/sessionHub");
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
