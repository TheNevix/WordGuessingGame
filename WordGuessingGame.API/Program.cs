using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WordGuessingGame.API.Hubs;
using WordGuessingGame.API.Models;
using WordGuessingGame.API.Services;
using WordGuessingGame.Repository.Extensions;

namespace WordGuessingGame.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Core services ──────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();

            // ── Database + repositories ────────────────────────────────
            builder.Services.AddRepository(builder.Configuration);

            // ── Auth services ──────────────────────────────────────────
            builder.Services.AddScoped<IAuthService, AuthService>();

            // ── JWT authentication ─────────────────────────────────────
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };

                    // SignalR sends the token as a query string param — read it here
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            var token = ctx.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) &&
                                ctx.HttpContext.Request.Path.StartsWithSegments("/gamehub"))
                            {
                                ctx.Token = token;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // ── Game singletons ────────────────────────────────────────
            builder.Services.AddSingleton<Lobby>();
            builder.Services.AddSingleton<GameService>();

            var words = File.ReadAllLines("words.txt");
            builder.Services.AddSingleton(new WordList(words));

            // ── CORS ───────────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithOrigins("http://localhost:5173", "https://raadhetwoord.be", "https://www.raadhetwoord.be");
                });
            });

            var app = builder.Build();

            // ── Middleware pipeline ────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<GameHub>("/gamehub");
            app.MapControllers();

            app.Run();
        }
    }
}
