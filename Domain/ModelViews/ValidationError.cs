namespace MinimalApi.Domain.ModelViews;

public struct ValidationError
{
    public List<string> Messages { get; set; }
}