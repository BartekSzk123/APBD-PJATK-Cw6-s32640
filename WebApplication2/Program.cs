using WebApplication2.Service;

namespace WebApplication2;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        
        builder.Services.AddOpenApi();
        builder.Services.AddScoped<IAppointmentService, AppoinmentService>();
        var app = builder.Build();
        
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "WebApplication2 v1"));
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers(); 
        
        app.Run();
    }
}