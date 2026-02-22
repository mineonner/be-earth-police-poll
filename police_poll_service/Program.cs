using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using police_poll_service.DB;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure to listen on port 5001
// builder.WebHost.UseUrls("http://+:5001");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy => policy.WithOrigins("https://police-poll-web.azurewebsites.net", "https://www.rtp-pss.com")
        // policy => policy.WithOrigins("http://localhost:4200", "http://www.rtp-pss.com")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

//conection Database
var connection = String.Empty;
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
    connection = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
}
else
{
    // ลองอ่านจาก Environment Variable ก่อน ถ้าไม่มีให้อ่านจาก appsettings.Production.json
    connection = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING")
                 ?? builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
}
builder.Services.AddDbContext<PolicePollDbContext>(options =>
    options.UseSqlServer(connection, providerOptions =>
    {
        providerOptions.CommandTimeout(180);
    }));

//JWT Authentication 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
    //app.UseSwaggerUI(options =>
    //{
    //    options.SwaggerEndpoint("/openapi/v1.json", "v1");
    //});
}

app.UseCors("AllowSpecificOrigin");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
