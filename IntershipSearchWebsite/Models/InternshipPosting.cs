namespace InternshipSearchWebsite.Models;

public class InternshipPosting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsRemote { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}