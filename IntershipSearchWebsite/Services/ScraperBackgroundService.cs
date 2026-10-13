using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack; // Bring this back!
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using InternshipSearchWebsite.Models;

namespace InternshipSearchWebsite.Services;

public class ScraperBackgroundService : BackgroundService
{
    private readonly InternshipSearchService _searchService;
    public ScraperBackgroundService(InternshipSearchService searchService)
    {
        _searchService = searchService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(30)); 
        
        // Keep HttpClient outside the loop
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "CSharp-Internship-Scraper");
        
        
        do {
            Console.WriteLine($"\n[{DateTime.Now}] Scraper is booting up...");

            try 
            {
                Console.WriteLine("Downloading Raw GitHub Internship List...");
                
                string rawUrl = "https://raw.githubusercontent.com/SimplifyJobs/Summer2027-Internships/dev/README.md";
                string rawContent = await client.GetStringAsync(rawUrl, stoppingToken);

                // Load the raw markdown string into HtmlAgilityPack
                var doc = new HtmlDocument();
                doc.LoadHtml(rawContent);

                // Find all table rows inside a table body
                var rows = doc.DocumentNode.SelectNodes("//tbody/tr");
                
                if (rows != null)
                {
                    Console.WriteLine($"Success! Found {rows.Count} rows.");
                    
                    List<InternshipPosting> internshipPostings = new();
                    int foundCount = 0;
                    string lastCompany = "Unknown Company";

                    foreach (var row in rows)
                    {
                        var cols = row.SelectNodes("td");
                        
                        if (cols != null && cols.Count >= 4)
                        {
                            // 1. Get Company
                            string companyText = cols[0].InnerText.Trim();
                            
                            // Carry over the company name if they use the ↳ character
                            if (companyText == "↳" || string.IsNullOrEmpty(companyText))
                            {
                                companyText = lastCompany;
                            }
                            else
                            {
                                // Remove the 🔥 emoji and update lastCompany
                                companyText = companyText.Replace("🔥", "").Trim();
                                lastCompany = companyText;
                            }

                            // 2. Get Role
                            string roleText = cols[1].InnerText.Trim();

                            // 3. Get Location (Replace <br> with commas so "SF<br>NYC" becomes "SF, NYC")
                            string locationHtml = cols[2].InnerHtml.Replace("<br>", ", ").Replace("<br/>", ", ");
                            
                            // Reload into a temporary node to strip the rest of the HTML (like <details> tags)
                            var tempDoc = new HtmlDocument();
                            tempDoc.LoadHtml(locationHtml);
                            string locationText = tempDoc.DocumentNode.InnerText.Trim();
                            
                            // Optional: clean up the "4 locations" prefix from the dropdown
                            if (locationText.Contains("locations"))
                            {
                                // Splits "4 locationsSF, Bellevue" into just the locations
                                locationText = locationText.Substring(locationText.IndexOf("locations") + 9).Trim();
                            }

                            // 4. Get Application Link
                            // Selects the first <a> tag inside the 4th column
                            var linkNode = cols[3].SelectSingleNode(".//a");
                            string applyLink = linkNode != null ? linkNode.GetAttributeValue("href", "No Link") : "Closed";

                            Console.WriteLine($"Found: {roleText} at {companyText} | Loc: {locationText}");
                            // Console.WriteLine($"Link: {applyLink}");

                            foundCount++;

                            // Stage 2 Database Logic:
                            var job = new InternshipPosting { 
                                Company = companyText, 
                                Title = roleText, 
                                Location = locationText,
                                Url = applyLink
                            };
                            internshipPostings.Add(job);

                        }
                    }

                    _searchService.UpdatePostings(internshipPostings);
                    Console.WriteLine($"Successfully loaded {internshipPostings.Count} jobs into memory.");
                }
                else 
                {
                    Console.WriteLine("Warning: Could not find any table rows in the document.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scraping failed: {ex.Message}");
            }

            Console.WriteLine($"[{DateTime.Now}] Scraper going back to sleep.");
        } while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}