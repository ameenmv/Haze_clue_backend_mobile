using HazeClue.Core.Domain.Entities;
using HazeClue.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace HazeClue.UI.Controllers.v1
{
    [Authorize]
    [ApiVersion("1.0")]
    public class InsightsController : CustomControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InsightsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetInsights()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var today = DateTime.UtcNow.Date;
            var recentInsights = await _context.UserInsights
                .Where(x => x.UserId == userId && x.CreatedAt >= today)
                .ToListAsync();

            // Generate new insights if we don't have any for today
            if (!recentInsights.Any())
            {
                await GenerateInsightsAsync(userId);
                recentInsights = await _context.UserInsights
                    .Where(x => x.UserId == userId && x.CreatedAt >= today)
                    .ToListAsync();
            }

            return Ok(recentInsights);
        }

        private async Task GenerateInsightsAsync(string userId)
        {
            var insights = new List<UserInsight>();
            
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            var watchData = await _context.SmartwatchData
                .Where(x => x.UserId == userId && x.Timestamp >= lastWeek)
                .ToListAsync();

            var assessment = await _context.HealthAssessments
                .FirstOrDefaultAsync(x => x.UserId == userId);

            // Rule 1: Sleep Analysis
            if (watchData.Any(d => d.SleepScore != null))
            {
                var avgSleep = watchData.Where(d => d.SleepScore != null).Average(d => d.SleepScore);
                if (avgSleep < 6.0)
                {
                    insights.Add(new UserInsight
                    {
                        UserId = userId,
                        Type = InsightType.DailyTip,
                        Title = "Sleep Quality Alert",
                        Message = "We noticed your sleep score has been low recently. Try our pre-sleep relaxation exercises tonight to improve your sleep quality."
                    });
                }
                else if (avgSleep > 7.5)
                {
                    insights.Add(new UserInsight
                    {
                        UserId = userId,
                        Type = InsightType.DailyTip,
                        Title = "Great Sleep",
                        Message = "Your sleep quality has been excellent! Keep up your current evening routine."
                    });
                }
            }

            // Rule 2: Steps/Activity
            if (watchData.Any(d => d.Steps != null))
            {
                var avgSteps = watchData.Where(d => d.Steps != null).Average(d => d.Steps);
                if (avgSteps < 4000)
                {
                    insights.Add(new UserInsight
                    {
                        UserId = userId,
                        Type = InsightType.DailyTip,
                        Title = "Time to Move",
                        Message = "Your daily activity is a bit low this week. Even a 15-minute walk can boost your focus and mood!"
                    });
                }
            }
            
            // Rule 3: Integration with Assessment
            if (assessment != null && !string.IsNullOrEmpty(assessment.AssessmentDataJson))
            {
                try 
                {
                    var doc = JsonDocument.Parse(assessment.AssessmentDataJson);
                    // Check if user reported high stress during onboarding
                    if (doc.RootElement.TryGetProperty("stress_level", out var stressProp) && stressProp.GetString() == "high")
                    {
                         var avgHrv = watchData.Where(d => d.Hrv != null).Select(d => d.Hrv).DefaultIfEmpty(0).Average();
                         // Low HRV indicates physiological stress
                         if (avgHrv > 0 && avgHrv < 40) 
                         {
                             insights.Add(new UserInsight
                             {
                                 UserId = userId,
                                 Type = InsightType.Alert,
                                 Title = "High Stress Detected",
                                 Message = "Based on your initial assessment and your recent Heart Rate Variability, your body is experiencing high stress. Taking a short tDCS or Focus session is highly recommended."
                             });
                         }
                    }
                } catch { }
            }

            // Fallback tip if no specific rules matched
            if (!insights.Any())
            {
                 insights.Add(new UserInsight
                 {
                     UserId = userId,
                     Type = InsightType.DailyTip,
                     Title = "Daily Health Tip",
                     Message = "Staying hydrated and taking short breaks throughout the day helps maintain your focus and energy levels."
                 });
            }

            _context.UserInsights.AddRange(insights);
            await _context.SaveChangesAsync();
        }
    }
}
