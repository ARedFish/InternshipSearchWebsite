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

    public void UpdatePostings(List<InternshipPosting> newPostings)
    {
        _internshipPostings = newPostings; 

        OnDataUpdated?.Invoke();
    }

    public Task<List<InternshipPosting>> SearchAsync(string keyword)
    {
        var query = _internshipPostings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(job => 
                (job.Title != null && job.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (job.Company != null && job.Company.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (job.Url != null && job.Url.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                ("advanced degree required".Contains(keyword, StringComparison.OrdinalIgnoreCase) && job.AdvancedDegreeRequired)
            );
        }

        var results = query.ToList();
        return Task.FromResult(results);
    }
}