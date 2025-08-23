using MinimalApi.Domain.Entities;
using MinimalApi.DTOs;

namespace MinimalApi.Domain.Interfaces;

public interface IAdminService
{
    Admin? Login(LoginDTO loginDTO);
}