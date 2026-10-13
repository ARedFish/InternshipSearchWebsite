using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipSearchWebsite.Models;

namespace InternshipSearchWebsite.Services;

public class InternshipSearchService
{
    public event Action? OnDataUpdated;
    private List<InternshipPosting> _internshipPostings = new();

    // 1. The Scraper calls this to swap in the fresh data
    public void UpdatePostings(List<InternshipPosting> newPostings)
    {
        // This is an atomic operation, making it thread-safe for web traffic
        _internshipPostings = newPostings; 

        OnDataUpdated?.Invoke();
    }

    // 2. The Web UI calls this to get the data
    public Task<List<InternshipPosting>> SearchAsync(string keyword)
    {
        var query = _internshipPostings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(job => 
                (job.Title != null && job.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (job.Company != null && job.Company.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                // Add your other property checks here
            );
        }

        var results = query.ToList();
        return Task.FromResult(results);
    }
}