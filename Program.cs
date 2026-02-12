using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Trasgo.Shared.ViewModels;
using Microsoft.AspNetCore.Http.Features;
using RepositoryPattern.Services.OtpService;
using RepositoryPattern.Services.AuthService;
using RepositoryPattern.Services.AttachmentService;
using RepositoryPattern.Services.RoleService;
using RepositoryPattern.Services.ArisanService;
using RepositoryPattern.Services.RekeningService;
using RepositoryPattern.Services.ChatService;
using RepositoryPattern.Services.PatunganService;
using RepositoryPattern.Services.TransaksiService;
using RepositoryPattern.Services.UserService;
using RepositoryPattern.Services.BannerService;
using RepositoryPattern.Services.SettingService;
using RepositoryPattern.Services.EventService;
using RepositoryPattern.Services.OrderService;
using RepositoryPattern.Services.RedPayService;

var builder = WebApplication.CreateBuilder(args);

// Pastikan listen di semua interface
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Dependency Injection
builder.Services.AddSingleton<ConvertJWT>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IArisanService, ArisanService>();
builder.Services.AddScoped<IPatunganService, PatunganService>();
builder.Services.AddScoped<ITransaksiService, TransaksiService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IRekeningService, RekeningService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IRedPayService, RedPayService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var configBuilder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    IConfigurationRoot configuration = configBuilder.Build();
    string secretKey = configuration.GetSection("AppSettings")["JwtKey"];

    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ClockSkew = TimeSpan.Zero,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "Beres.com",
        ValidAudience = "Beres.com",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Swagger Config
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Beres", Version = "v1" });
    c.OperationFilter<SwaggerFileOperationFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// File upload limit
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 300 * 1024 * 1024; // 300 MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 300 * 1024 * 1024; // 300 MB
});

// Build App
var app = builder.Build();

// Routing & Middleware order
app.UseRouting();

app.UseCors("AllowAll");

// 🔹 Swagger Aktif untuk semua environment
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Beres API V1");
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (maxRequestBodySizeFeature != null)
    {
        maxRequestBodySizeFeature.MaxRequestBodySize = 200 * 1024 * 1024;
    }

    await next();

    if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
    {
        context.Response.ContentType = "application/json";
        var viewModel = new
        {
            code = 401,
            errorMessage = new ErrorDtoVM { error = MessageReport.Unauthorized }
        };
        var json = JsonSerializer.Serialize(viewModel);
        await context.Response.WriteAsync(json);
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
