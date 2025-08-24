using MinimalApi.Domain.Entities;
using MinimalApi.DTOs;

namespace MinimalApi.Domain.Interfaces;

public interface IAdminService
{
    List<Admin> All(int? page);
    Admin? SearchId(int id);
    Admin? Login(LoginDTO loginDTO);
    Admin Include(Admin admin);
}