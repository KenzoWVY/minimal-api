using MinimalApi.Domain.Enums;

namespace MinimalApi.DTOs;

public class AdminDTO
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public Profile? Profle { get; set; } = default!;   
}
