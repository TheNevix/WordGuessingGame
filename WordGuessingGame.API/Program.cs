
using WordGuessingGame.API.Models;
using WordGuessingGame.API.Services;

namespace WordGuessingGame.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<GameState>();
            builder.Services.AddScoped<GameService>();

            var words = File.ReadAllLines("words.txt");
            builder.Services.AddSingleton(new WordList(words));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Create hubs here
            //app.MapHub<GameHub>("/gamehub");

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
