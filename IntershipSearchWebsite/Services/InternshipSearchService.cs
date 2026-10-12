using InternshipSearchWebsite.Models;

namespace InternshipSearchWebsite.Services;

public class InternshipSearchService
{
    // 1. The Mock Data
    private readonly List<InternshipPosting> _mockDatabase = new()
    {
        new InternshipPosting {
            Id = 1,
            Title = "Backend Engineering Intern",
            Company = "TechNova",
            Location = "New York, NY",
            IsRemote = false,
            Description = "Work on our core backend systems and APIs using C# and .NET.",
            Deadline = new DateTime(2026, 11, 15),
            Tags = new List<string> { "C#", ".NET", "Backend" }
        },
        new InternshipPosting {
            Id = 2,
            Title = "Full Stack Developer Intern",
            Company = "CloudSphere",
            Location = "Remote",
            IsRemote = true,
            Description = "Build responsive UI components and connect them to cloud databases.",
            Deadline = new DateTime(2026, 12, 1),
            Tags = new List<string> { "Blazor", "C#", "Azure" }
        },
        new InternshipPosting {
            Id = 3,
            Title = "Data Science Intern",
            Company = "DataCorp",
            Location = "Boston, MA",
            IsRemote = false,
            Description = "Analyze large datasets to find trends and build predictive models.",
            Deadline = new DateTime(2026, 10, 30),
            Tags = new List<string> { "Python", "SQL", "Machine Learning" }
        }
    };

    // 2. The Search Logic
    public Task<List<InternshipPosting>> SearchAsync(string keyword)
    {
        var query = _mockDatabase.AsQueryable();

        // If the user actually typed something, filter the list
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(job => 
                job.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                job.Company.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                job.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                job.Tags.Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            );
        }

        // Sort results so the closest deadlines appear at the top
        var results = query.OrderBy(job => job.Deadline).ToList();
        
        // Return as a Task to mimic how a real database operates asynchronously
        return Task.FromResult(results);
    }
}