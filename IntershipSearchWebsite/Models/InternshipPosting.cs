namespace InternshipSearchWebsite.Models;

public class InternshipPosting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsRemote { get; set; }
    public string Description { get; set; } = string.Empty;
    // public DateTime? Deadline { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Url { get; set; } = string.Empty;
    public bool AdvancedDegreeRequired {get; set; }
}