using Microsoft.EntityFrameworkCore;
using MinimalApi.Domain.Entities;
using MinimalApi.Domain.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infrastructure.DB;

namespace MinimalApi.Domain.Services;

public class VehicleService : IVehicleService
{
    private readonly AppDbContext _context;
    public VehicleService(AppDbContext context)
    {
        _context = context;
    }

    public List<Vehicle> All(int? page = 1, string? name = null, string? carBrand = null)
    {
        var query = _context.Vehicles.AsQueryable();
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(v => EF.Functions.Like(v.Name.ToLower(), $"%{name.ToLower()}%"));
        }

        int itensByPage = 10;
        if (page != null)
        {
            query = query.Skip(((int)page - 1) * itensByPage).Take(itensByPage);
        }
        
        return query.ToList();
    }

    public void Include(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        _context.SaveChanges();
    }

    public void Remove(Vehicle vehicle)
    {
        _context.Vehicles.Remove(vehicle);
        _context.SaveChanges();
    }

    public Vehicle? SearchId(int id)
    {
        return _context.Vehicles.Where(v => v.Id == id).FirstOrDefault();
    }

    public void Update(Vehicle vehicle)
    {
        _context.Vehicles.Update(vehicle);
        _context.SaveChanges();
    }
}