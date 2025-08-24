using Microsoft.EntityFrameworkCore;
using MinimalApi.Domain.Entities;
using MinimalApi.Domain.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infrastructure.DB;

namespace MinimalApi.Domain.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    public List<Admin> All(int? page)
    {
        var query = _context.Admins.AsQueryable();

        int itensByPage = 10;
        if (page != null)
        {
            query = query.Skip(((int)page - 1) * itensByPage).Take(itensByPage);
        }
        
        return query.ToList();
    }

    public Admin? SearchId(int id)
    {
        return _context.Admins.Where(ad => ad.Id == id).FirstOrDefault();
    }

    public Admin Include(Admin admin)
    {
        _context.Admins.Add(admin);
        _context.SaveChanges();

        return admin;
    }

    public Admin? Login(LoginDTO loginDTO)
    {
        return _context.Admins.Where(a => a.Email == loginDTO.Email && a.Password == loginDTO.Password).FirstOrDefault();
    }
}