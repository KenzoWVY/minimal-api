namespace MinimalApi.Domain.ModelViews;

public record AdmLoggedIn
{
    public string Email { get; set; } = default!;
    public string Profile { get; set; } = default!;
    public string Token { get; set; } = default!;
}