
namespace Avemel.HR.Domain.Entities;

public class Division
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string LegalName { get; set; } = string.Empty;

    public string Colour { get; set; } = "#0f766e";

    public bool Active { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
