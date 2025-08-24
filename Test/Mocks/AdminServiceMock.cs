using System.ComponentModel.DataAnnotations;
using MinimalApi.Domain.Entities;
using MinimalApi.Domain.Interfaces;
using MinimalApi.DTOs;

namespace Test.Mocks;

public class AdminServiceMock : IAdminService
{
    private static List<Admin> admins = new List<Admin>()
    {
        new Admin
        {
            Id = 1,
            Email = "admin@test.com" ,
            Password = "123456",
            Profile = "Adm"
        },
        new Admin
        {
            Id = 2,
            Email = "admin2@test2.com" ,
            Password = "123456",
            Profile = "Editor"
        }
    };
    public List<Admin> All(int? page)
    {
        return admins;
    }

    public Admin Include(Admin admin)
    {
        admin.Id = admins.Count() + 1;
        admins.Add(admin);

        return admin;
    }

    public Admin? Login(LoginDTO loginDTO)
    {
        return admins.Find(ad => ad.Email == loginDTO.Email && ad.Password == loginDTO.Password);
    }

    public Admin? SearchId(int id)
    {
        return admins.Find(a => a.Id == id);
    }
}