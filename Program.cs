using Microsoft.EntityFrameworkCore;
using MinimalApi.Infrastructure.DB;
using MinimalApi.DTOs;
using MinimalApi.Domain.Interfaces;
using MinimalApi.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Domain.ModelViews;
using MinimalApi.Domain.Entities;
using MinimalApi.Domain.Enums;
using System.ComponentModel;
using System.Runtime.InteropServices.Marshalling;

Console.WriteLine(typeof(IAdminService).FullName);
Console.WriteLine(typeof(IAdminService).Assembly.FullName);

#region Builder

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
    );
});

var app = builder.Build();

#endregion

#region Home

app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");

#endregion

#region Admins

app.MapPost("/admins/login", ([FromBody] LoginDTO loginDTO, IAdminService adminService) =>
{
    if (adminService.Login(loginDTO) != null)
        return Results.Ok("Sucessful login");
    else
        return Results.Unauthorized();
}).WithTags("Admins");

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
}).WithTags("Admins");

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
}).WithTags("Admins");

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
}).WithTags("Admins");

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
}).WithTags("Vehicles");

app.MapGet("/vehicles", ([FromQuery] int? page, IVehicleService vehicleService) =>
{
    return Results.Ok(vehicleService.All(page));
}).WithTags("Vehicles");

app.MapGet("/vehicles/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
    var vehicle = vehicleService.SearchId(id);
    if (vehicle == null) return Results.NotFound();

    return Results.Ok(vehicle);
}).WithTags("Vehicles");

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
}).WithTags("Vehicles");

app.MapDelete("/vehicles/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
    var vehicle = vehicleService.SearchId(id);
    if (vehicle == null) return Results.NotFound();

    vehicleService.Remove(vehicle);

    return Results.NoContent();
}).WithTags("Vehicles");

#endregion

#region App

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

#endregion