using Microsoft.EntityFrameworkCore;
using MinimalApi.Infrastructure.DB;
using MinimalApi.DTOs;
using MinimalApi.Domain.Interfaces;
using MinimalApi.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Domain.ModelViews;
using MinimalApi.Domain.Entities;
using MinimalApi.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

Console.WriteLine(typeof(IAdminService).FullName);
Console.WriteLine(typeof(IAdminService).Assembly.FullName);

#region Builder

var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key)) key = "12345";

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insert JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("MySql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySql"))
    );
});

var app = builder.Build();

#endregion

#region Home

app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");

#endregion

#region Admins

string GenJwtToken(Admin admin)
{
    if (string.IsNullOrEmpty(key)) return string.Empty;

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new Claim("Email", admin.Email),
        new Claim("Profile", admin.Profile),
        new Claim(ClaimTypes.Role, admin.Profile)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Login
app.MapPost("/admins/login", ([FromBody] LoginDTO loginDTO, IAdminService adminService) =>
{
    var adm = adminService.Login(loginDTO);
    if (adm != null)
    {
        string token = GenJwtToken(adm);
        return Results.Ok(new AdmLoggedIn
        {
            Email = adm.Email,
            Profile = adm.Profile,
            Token = token
        });
    }
    else
        return Results.Unauthorized();
}).AllowAnonymous().WithTags("Admins");

// Add admin
app.MapPost("/admins", ([FromBody] AdminDTO adminDTO, IAdminService adminService) =>
{
    var validation = new ValidationError
    {
        Messages = new List<string>()
    };

    if (string.IsNullOrEmpty(adminDTO.Email))
        validation.Messages.Add("Email cannot be empty");
    if (string.IsNullOrEmpty(adminDTO.Password))
        validation.Messages.Add("Password cannot be empty");
    if (adminDTO.Profle == null)
        validation.Messages.Add("Profile cannot be empty");
    if (validation.Messages.Count() > 0)
        return Results.BadRequest(validation);

    var admin = new Admin
    {
        Email = adminDTO.Email,
        Password = adminDTO.Password,
        Profile = adminDTO.Profle?.ToString() ?? Profile.Editor.ToString()
    };
    adminService.Include(admin);

    return Results.Created($"/admin/{admin.Id}", new AdminModelView
        {
            Id = admin.Id,
            Email = admin.Email,
            Profile = admin.Profile
        });
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"}).WithTags("Admins");

// List admins
app.MapGet("/admins", ([FromQuery] int? page, IAdminService adminService) =>
{
    var adms = new List<AdminModelView>();
    var admins = adminService.All(page);

    foreach (var adm in admins)
    {
        adms.Add(new AdminModelView
        {
            Id = adm.Id,
            Email = adm.Email,
            Profile = adm.Profile
        });
    }
    return Results.Ok(adms);
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"}).WithTags("Admins");

// View admin
app.MapGet("/admins/{id}", ([FromRoute] int id, IAdminService adminService) =>
{
    var admin = adminService.SearchId(id);
    if (admin == null) return Results.NotFound();

    return Results.Ok(new AdminModelView
    {
        Id = admin.Id,
        Email = admin.Email,
        Profile = admin.Profile
    });
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"}).WithTags("Admins");

#endregion

#region Vehicles

ValidationError validateDTO(VehicleDTO vehicleDTO)
{
    var validation = new ValidationError
    {
        Messages = new List<string>()
    };

    if (string.IsNullOrEmpty(vehicleDTO.Name))
        validation.Messages.Add("Name cannot be empty");
    if (string.IsNullOrEmpty(vehicleDTO.CarBrand))
        validation.Messages.Add("Brand cannot be empty");
    if (vehicleDTO.Year < 1950)
        validation.Messages.Add("Only accepted years after 1950");

    return validation;
}

// Add vehicle
app.MapPost("/vehicles", ([FromBody] VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
{
    var validation = validateDTO(vehicleDTO);
    if (validation.Messages.Count() > 0)
        return Results.BadRequest(validation);

    var vehicle = new Vehicle
    {
        Name = vehicleDTO.Name,
        CarBrand = vehicleDTO.CarBrand,
        Year = vehicleDTO.Year,
    };
    vehicleService.Include(vehicle);

    return Results.Created($"/vehicle/{vehicle.Id}", vehicle);
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm,Editor"}).WithTags("Vehicles");

// List vehicles
app.MapGet("/vehicles", ([FromQuery] int? page, IVehicleService vehicleService) =>
{
    return Results.Ok(vehicleService.All(page));
}).RequireAuthorization().WithTags("Vehicles");

// View vehicle
app.MapGet("/vehicles/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
    var vehicle = vehicleService.SearchId(id);
    if (vehicle == null) return Results.NotFound();

    return Results.Ok(vehicle);
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm,Editor"}).WithTags("Vehicles");

// Edit vehicle
app.MapPut("/vehicles/{id}", ([FromRoute] int id, VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
{
    var validation = validateDTO(vehicleDTO);
    if (validation.Messages.Count() > 0)
        return Results.BadRequest(validation);

    var vehicle = vehicleService.SearchId(id);
    if (vehicle == null) return Results.NotFound();

    vehicle.Name = vehicleDTO.Name;
    vehicle.CarBrand = vehicleDTO.CarBrand;
    vehicle.Year = vehicleDTO.Year;
    vehicleService.Update(vehicle);

    return Results.Ok();
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"}).WithTags("Vehicles");

// Remove vehicle
app.MapDelete("/vehicles/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
    var vehicle = vehicleService.SearchId(id);
    if (vehicle == null) return Results.NotFound();

    vehicleService.Remove(vehicle);

    return Results.NoContent();
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"}).WithTags("Vehicles");

#endregion

#region App

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

#endregion