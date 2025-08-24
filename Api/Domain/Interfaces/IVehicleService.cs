using MinimalApi.Domain.Entities;
using MinimalApi.DTOs;

namespace MinimalApi.Domain.Interfaces;

public interface IVehicleService
{
    List<Vehicle> All(int? page = 1, string? name = null, string? carBrand = null);
    Vehicle? SearchId(int id);
    void Include(Vehicle vehicle);
    void Update(Vehicle vehicle);
    void Remove(Vehicle vehicle);
}