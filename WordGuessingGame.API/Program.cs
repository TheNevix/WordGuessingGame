using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Display;
using Serilog.Sinks.Grafana.Loki;
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
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            // ── Logging: Serilog → Console + Grafana Loki ──────────────
            // Logs are labelled with the runtime environment (Development locally, Production
            // on the server), so dev and prod are filterable in Grafana via {env="..."}.
            // Loki is only attached when its config is present; otherwise we just log to console.
            builder.Host.UseSerilog((context, loggerConfig) =>
            {
                // Levels come from the "Serilog" config section so verbosity can be tuned
                // (e.g. quiet EF queries down) without recompiling. Defaults to Information+
                // for everything, which mirrors the full console output incl. SQL and errors.
                loggerConfig
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();

                var lokiUrl = context.Configuration["Loki:Url"];
                var lokiUser = context.Configuration["Loki:User"];
                var lokiToken = context.Configuration["Loki:Token"];
                if (!string.IsNullOrWhiteSpace(lokiUrl) && !string.IsNullOrWhiteSpace(lokiToken))
                {
                    loggerConfig.WriteTo.GrafanaLoki(
                        lokiUrl,
                        labels: new[]
                        {
                            new LokiLabel { Key = "app", Value = "raad-api" },
                            new LokiLabel { Key = "env", Value = context.HostingEnvironment.EnvironmentName },
                        },
                        credentials: new LokiCredentials { Login = lokiUser, Password = lokiToken },
                        // Send the rendered message (not the default JSON event) so log lines
                        // read like the console instead of a raw JSON blob.
                        textFormatter: new MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}"));
                }
            });

            // ── Core services ──────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();

            // ── Database + repositories ────────────────────────────────
            builder.Services.AddRepository(builder.Configuration);

            // ── Auth services ──────────────────────────────────────────
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IEmailService, EmailService>();
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
                        .WithOrigins(
                            "http://localhost:5173",
                            "https://raadhetwoord.be",
                            "http://raadhetwoord.be",
                            "https://www.raadhetwoord.be",
                            "http://www.raadhetwoord.be");
                });
            });

            var app = builder.Build();

            // Concise one-line-per-request HTTP logging
            app.UseSerilogRequestLogging();

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
