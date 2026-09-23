using GameAnalytics.Data;
using GameAnalytics.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Services
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<EventIngestionService>();
builder.Services.AddScoped<JwtService>();

// JWT Auth
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSection["SecretKey"] ?? throw new Exception("JwtSettings:SecretKey is not configured.");
var issuer = jwtSection["Issuer"] ?? "GameAnalytics";
var audience = jwtSection["Audience"] ?? "GameAnalyticsDashboard";

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = issuer,
			ValidAudience = audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
			ClockSkew = TimeSpan.FromMinutes(1),
		};
	});

builder.Services.AddAuthorization();

// CORS — origins from config
var allowedOrigins = builder.Configuration
	.GetSection("Cors:AllowedOrigins")
	.Get<string[]>() ?? [""];

Console.WriteLine(string.Join(", ", allowedOrigins));

builder.Services.AddCors(options =>
	options.AddPolicy("VueDev", policy =>
		policy.WithOrigins(allowedOrigins)
			  .AllowAnyHeader()
			  .AllowAnyMethod()));

// Controllers + Swagger
builder.Services.AddControllers()
	.AddJsonOptions(o =>
		o.JsonSerializerOptions.Converters.Add(
			new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new() { Title = "GameAnalytics API", Version = "v1" });

	c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = Microsoft.OpenApi.Models.ParameterLocation.Header,
		Description = "Enter your JWT token. Example: Bearer eyJhbG..."
	});

	c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Name = "X-Api-Key",
		Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
		In = Microsoft.OpenApi.Models.ParameterLocation.Header,
		Description = "SDK API Key for ingestion endpoints"
	});

	c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
	{
		{
			new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Reference = new Microsoft.OpenApi.Models.OpenApiReference
				{
					Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
					Id   = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});

	c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
	{
		{
			new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Reference = new Microsoft.OpenApi.Models.OpenApiReference
				{
					Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
					Id   = "ApiKey"
				}
			},
			Array.Empty<string>()
		}
	});
});

var app = builder.Build();

// Migrations on startup 
using (var scope = app.Services.CreateScope())
{
	var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	dbCtx.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("VueDev");
app.UseAuthorization();
app.MapControllers();
app.Run();
