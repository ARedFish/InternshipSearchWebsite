using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
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
        using PeriodicTimer timer = new(TimeSpan.FromHours(1)); 
        
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "CSharp-Internship-Scraper");
        
        
        do {
            Console.WriteLine($"\n[{DateTime.Now}] Scraper is booting up...");

            try 
            {
                Console.WriteLine("Downloading Raw GitHub Internship List...");
                
                string rawUrl = "https://raw.githubusercontent.com/SimplifyJobs/Summer2027-Internships/dev/README.md";
                string rawContent = await client.GetStringAsync(rawUrl, stoppingToken);

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
                            // Get Company
                            string companyText = cols[0].InnerText.Trim();
                            bool advancedDegreeRequired = false;
                            bool isRemote = false;
                            
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

                            // Get Role
                            string roleText = cols[1].InnerText.Trim();

                            if (roleText.Contains("🎓"))
                            {
                                roleText = roleText.Replace("🎓", "");
                                advancedDegreeRequired = true;
                            }

                            // Get Location 
                            string locationHtml = cols[2].InnerHtml.Replace("<br>", ", ").Replace("<br/>", ", ");
                            
                            // Strip the rest of the HTML 
                            var tempDoc = new HtmlDocument();
                            tempDoc.LoadHtml(locationHtml);
                            string locationText = tempDoc.DocumentNode.InnerText.Trim();
                            
                            // Remove 4 locations prefix
                            if (locationText.Contains("locations"))
                            {
                                locationText = locationText.Substring(locationText.IndexOf("locations") + 9).Trim();
                            }

                            // Check if remote
                            if (locationText.Contains("remote", StringComparison.OrdinalIgnoreCase))
                            {
                                isRemote = true;
                            }

                            var linkNode = cols[3].SelectSingleNode(".//a");
                            string applyLink = linkNode != null ? linkNode.GetAttributeValue("href", "No Link") : "Closed";

                            Console.WriteLine($"Found: {roleText} at {companyText} | Loc: {locationText}");

                            foundCount++;

                            var job = new InternshipPosting { 
                                Company = companyText, 
                                Title = roleText, 
                                Location = locationText,
                                Url = applyLink,
                                AdvancedDegreeRequired = advancedDegreeRequired,
                                IsRemote = isRemote
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