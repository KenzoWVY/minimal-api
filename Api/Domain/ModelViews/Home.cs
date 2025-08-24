namespace MinimalApi.Domain.ModelViews;

public struct Home
{
    public string Message { get => "Thanks for using the vehicle API - Minimal API"; }
    public string Documentation { get => "/swagger"; }
}