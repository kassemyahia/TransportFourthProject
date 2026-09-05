using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Services;
using TransportFourthProject.Api.Settings;
using TransportFourthProject.Api.Services.Payments;
using TransportFourthProject.Api.Services.Pricing;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace TransportFourthProject.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            #region JWT

            var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                ?? throw new Exception("Jwt settings missing!");
            builder.Services.AddSingleton(jwtSettings);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                      Encoding.UTF8.GetBytes(jwtSettings.Key))
                };
            });
            builder.Services.AddAuthorization();
            #endregion JWT

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            // Add services to the container.
            //builder.Services.AddControllers().AddNewtonsoftJson(options =>
            //{
            //    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            //});
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TransportFourthProject.Api", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter JWT token: Bearer {your token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                { {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { } }
                });
                c.UseInlineDefinitionsForEnums();
            });

            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<ITripRepository, TripRepository>();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddSingleton<PasswordHasher>();
            builder.Services.AddSingleton<AesEncryptionService>();
            builder.Services.AddScoped<FakePaymentGateway>();
            builder.Services.AddScoped<PaymentService>();
            builder.Services.AddScoped<PriceCalculatorService>();
            builder.Services.AddHostedService<BookingCleanupService>();
            builder.Services.AddScoped<IEmployeeCityRepository, EmployeeCityRepository>();
            builder.Services.AddScoped<IEmployeeBusTypeRepository, EmployeeBusTypeRepository>();
            builder.Services.AddScoped<IEmployeeTripRepository, EmployeeTripRepository>();
            builder.Services.AddScoped<IEmployeeBusRepository, EmployeeBusRepository>();
            builder.Services.AddScoped<IEmployeeRoutePriceRepository, EmployeeRoutePriceRepository>();
            builder.Services.AddHostedService<TripArrivalService>();
            builder.Services.AddScoped<IAdminUserDiscountTicketRepo, AdminUserDiscountTicketRepository>();
            builder.Services.AddScoped<IAdminTripDiscountRepository, AdminTripDiscountRepository>();
            builder.Services.AddScoped<IAdminUserDiscountRepository, AdminUserDiscountRepository>();
            builder.Services.AddScoped<IAdminTripRepo, AdminTripRepo>();
            builder.Services.AddScoped<IDriverDashboardRepo, DriverDashboardRepo>();
            builder.Services.AddScoped<IAdminAllOperationOnEmployeeTableRepo, AdminAllOperationOnEmployeeTableRepo>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}


//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// 2. إعداد الـ Controllers (استدعاء واحد فقط!)
//// سنستخدم NewtonsoftJson لأنه يدعم ميزات أكثر في المشاريع المعقدة
//builder.Services.AddControllers()
//    .AddNewtonsoftJson(options =>
//    {
//        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
//    });

//// 3. إعداد Swagger
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TransportFourthProject.Api", Version = "v1" });

//    // إعدادات الـ Security
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "Enter JWT token: Bearer {your token}",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
//            },
//            new string[] { }
//        }
//    });

//    // لضمان ظهور الـ Enums كنصوص في Swagger
//    c.UseInlineDefinitionsForEnums();
//});

//// 4. تسجيل الخدمات (Dependency Injection)
//builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
//builder.Services.AddScoped<ITripRepository, TripRepository>();
//builder.Services.AddScoped<TokenService>();
//builder.Services.AddSingleton<PasswordHasher>();
//builder.Services.AddSingleton<AesEncryptionService>();
//builder.Services.AddScoped<FakePaymentGateway>();
//builder.Services.AddScoped<PaymentService>();
//builder.Services.AddScoped<PriceCalculatorService>();
//builder.Services.AddHostedService<BookingCleanupService>();

//// 5. إيقاف الفلترة التلقائية للتحكم في رسائل الخطأ (التي ناقشناها سابقاً)
//builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
//{
//    options.SuppressModelStateInvalidFilter = true;
//});

//// 6. بناء التطبيق وإعداد الـ Middleware
//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();
//app.MapControllers();

//app.Run();
