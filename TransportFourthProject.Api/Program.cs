using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using System.Text;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Services;
using TransportFourthProject.Api.Services.Payments;
using TransportFourthProject.Api.Services.Pricing;
using TransportFourthProject.Api.Settings;

namespace TransportFourthProject.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================================================
            // JWT
            // =========================================================

            var jwtSettings = builder.Configuration
                .GetSection("Jwt")
                .Get<JwtSettings>()
                ?? throw new Exception("Jwt settings missing!");

            builder.Services.AddSingleton(jwtSettings);

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            ValidIssuer = jwtSettings.Issuer,
                            ValidAudience = jwtSettings.Audience,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(jwtSettings.Key)
                                )
                        };
                });

            builder.Services.AddAuthorization();

            // =========================================================
            // DATABASE
            // =========================================================

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString(
                        "DefaultConnection"
                    )
                );
            });

            // =========================================================
            // CORS
            // =========================================================

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // =========================================================
            // CONTROLLERS / JSON
            // =========================================================

            builder.Services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(
                        new StringEnumConverter()
                    );
                });

            // =========================================================
            // SWAGGER
            // =========================================================

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "TransportFourthProject.Api",
                        Version = "v1"
                    }
                );

                c.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description =
                            "Enter JWT token: Bearer {your token}",

                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    }
                );

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
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
                            Array.Empty<string>()
                        }
                    }
                );

                c.UseInlineDefinitionsForEnums();
            });

            // =========================================================
            // DEPENDENCY INJECTION
            // =========================================================

            builder.Services.AddScoped(
                typeof(IRepository<>),
                typeof(Repository<>)
            );

            builder.Services.AddScoped<ITripRepository, TripRepository>();

            builder.Services.AddScoped<TokenService>();

            builder.Services.AddSingleton<PasswordHasher>();
            builder.Services.AddSingleton<AesEncryptionService>();

            builder.Services.AddScoped<FakePaymentGateway>();
            builder.Services.AddScoped<PaymentService>();
            builder.Services.AddScoped<PriceCalculatorService>();

            builder.Services.AddHostedService<BookingCleanupService>();

            builder.Services.AddScoped<
                IEmployeeCityRepository,
                EmployeeCityRepository
            >();

            builder.Services.AddScoped<
                IEmployeeBusTypeRepository,
                EmployeeBusTypeRepository
            >();

            builder.Services.AddScoped<
                IEmployeeTripRepository,
                EmployeeTripRepository
            >();

            builder.Services.AddScoped<
                IEmployeeBusRepository,
                EmployeeBusRepository
            >();

            builder.Services.AddScoped<
                IEmployeeRoutePriceRepository,
                EmployeeRoutePriceRepository
            >();

            builder.Services.AddHostedService<TripArrivalService>();

            builder.Services.AddScoped<
                IAdminUserDiscountTicketRepo,
                AdminUserDiscountTicketRepository
            >();

            builder.Services.AddScoped<
                IAdminTripDiscountRepository,
                AdminTripDiscountRepository
            >();

            builder.Services.AddScoped<
                IAdminUserDiscountRepository,
                AdminUserDiscountRepository
            >();

            builder.Services.AddScoped<
                IAdminTripRepo,
                AdminTripRepo
            >();

            builder.Services.AddScoped<
                IDriverDashboardRepo,
                DriverDashboardRepo
            >();

            builder.Services.AddScoped<
                IAdminAllOperationOnEmployeeTableRepo,
                AdminAllOperationOnEmployeeTableRepo
            >();

            // =========================================================
            // BUILD
            // =========================================================

            var app = builder.Build();

            // =========================================================
            // APPLY PENDING MIGRATIONS
            // =========================================================

            using (var scope = app.Services.CreateScope())
            {
                var db =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                db.Database.Migrate();
            }

            // =========================================================
            // MIDDLEWARE
            // =========================================================

            app.UseSwagger();
            app.UseSwaggerUI();

            /*
             * Keep CORS before authentication/authorization.
             * This is especially important for browser preflight OPTIONS
             * requests from your frontend.
             */
            app.UseCors("AllowFrontend");

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}