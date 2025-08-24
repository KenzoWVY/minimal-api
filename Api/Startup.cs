using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApi.Domain.Entities;
using MinimalApi.Domain.Enums;
using MinimalApi.Domain.Interfaces;
using MinimalApi.Domain.ModelViews;
using MinimalApi.Domain.Services;
using MinimalApi.DTOs;
using MinimalApi.Infrastructure.DB;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        key = Configuration.GetSection("Jwt").ToString() ?? "";
    }

    private string key;

    public IConfiguration Configuration { get; set; } = default!;

    public void ConfigureServices(IServiceCollection services)
    {


        services.AddAuthentication(option =>
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        services.AddAuthorization();

        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IVehicleService, VehicleService>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
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

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseMySql(
                Configuration.GetConnectionString("MySql"),
                ServerVersion.AutoDetect(Configuration.GetConnectionString("MySql"))
            );
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");

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
            endpoints.MapPost("/admins/login", ([FromBody] LoginDTO loginDTO, IAdminService adminService) =>
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
            endpoints.MapPost("/admins", ([FromBody] AdminDTO adminDTO, IAdminService adminService) =>
            {
                var validation = new ValidationError
                {
                    Messages = new List<string>()
                };

                if (string.IsNullOrEmpty(adminDTO.Email))
                    validation.Messages.Add("Email cannot be empty");
                if (string.IsNullOrEmpty(adminDTO.Password))
                    validation.Messages.Add("Password cannot be empty");
                if (adminDTO.Profile == null)
                    validation.Messages.Add("Profile cannot be empty");
                if (validation.Messages.Count() > 0)
                    return Results.BadRequest(validation);

                var admin = new Admin
                {
                    Email = adminDTO.Email,
                    Password = adminDTO.Password,
                    Profile = adminDTO.Profile?.ToString() ?? Profile.Editor.ToString()
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
            endpoints.MapGet("/admins", ([FromQuery] int? page, IAdminService adminService) =>
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
            endpoints.MapGet("/admins/{id}", ([FromRoute] int id, IAdminService adminService) =>
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
            endpoints.MapPost("/vehicles", ([FromBody] VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
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
            endpoints.MapGet("/vehicles", ([FromQuery] int? page, IVehicleService vehicleService) =>
            {
                return Results.Ok(vehicleService.All(page));
            }).RequireAuthorization().WithTags("Vehicles");

            // View vehicle
            endpoints.MapGet("/vehicles/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
            {
                var vehicle = vehicleService.SearchId(id);
                if (vehicle == null) return Results.NotFound();

                return Results.Ok(vehicle);
            }).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm,Editor"}).WithTags("Vehicles");

            // Edit vehicle
            endpoints.MapPut("/vehicles/{id}", ([FromRoute] int id, VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
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
            endpoints.MapDelete("/vehicles/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
            {
                var vehicle = vehicleService.SearchId(id);
                if (vehicle == null) return Results.NotFound();

                vehicleService.Remove(vehicle);

                return Results.NoContent();
            }).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"}).WithTags("Vehicles");

#endregion
        });
    }
}