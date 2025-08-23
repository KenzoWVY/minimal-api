namespace MinimalApi.DTOs;
public record VehicleDTO
{
    public string Name { get; set; } = default!;

    public string CarBrand { get; set; } = default!;

    public int Year { get; set; } = default!;
}
