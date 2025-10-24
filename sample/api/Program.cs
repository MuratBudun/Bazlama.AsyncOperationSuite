using Bazlama.AsyncOperationSuite.Extensions;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage;
using Bazlama.AsyncOperationSuite.Mvc.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add authentication and authorization if needed
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = "yourdomain.com",
			ValidAudience = "yourdomain.com",
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_super_secret_key"))
		};
	});
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddAsyncOperationSuiteMemoryStorage(builder.Configuration);
//builder.Services.AddAsyncOperationSuiteMSSQLStorage(builder.Configuration);
builder.Services.AddAsyncOperationSuiteService(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddAsyncOperationSuiteMvcAllControllers(requireAuthorization: false);
//builder.Services.AddAsyncOperationSuiteMvcOperationPayload(requireAuthorization: false);
//builder.Services.AddAsyncOperationSuiteMvcOperationPublish(requireAuthorization: false);
//builder.Services.AddAsyncOperationSuiteMvcOperationQuery(requireAuthorization: false);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
	options.AddPolicy(name: "all", policy =>
    {
        policy.AllowAnyOrigin();
    });
});

var app = builder.Build();
app.UseCors("all");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();