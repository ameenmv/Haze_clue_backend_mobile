using HazeClue.Core.Domain.Entities;
using HazeClue.Infrastructure.DbContext;
using HazeClue.UI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HazeClue.UI.Controllers.v1
{
    [Authorize]
    [ApiVersion("1.0")]
    public class SessionsController : CustomControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSessions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessions = await _context.Sessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return Ok(sessions);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var session = new FocusSession
            {
                UserId = userId!,
                Title = dto.Title,
                DurationMinutes = dto.DurationMinutes,
                DeviceId = dto.DeviceId,
                CreatedAt = DateTime.UtcNow,
                Status = "active"
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return Ok(session);
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteSession(string id, [FromBody] CompleteSessionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();

            session.Status = "completed";
            session.CompletedAt = DateTime.UtcNow;
            session.AverageConcentration = dto.AverageConcentration;
            session.ActualDurationSeconds = dto.ActualDurationSeconds;
            
            // Create a notification for session completion
            string durationText = dto.ActualDurationSeconds < 60 
                ? $"{dto.ActualDurationSeconds} seconds" 
                : $"{dto.ActualDurationSeconds / 60} minutes";

            var notification = new HazeClue.Core.Domain.Entities.AppNotification
            {
                UserId = userId,
                Title = "Session Completed! 🎉",
                Message = $"Great job! You just completed a focus session lasting {durationText} with an average concentration of {dto.AverageConcentration}%.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            return Ok(session);
        }

        [HttpPost("{id}/pause")]
        public async Task<IActionResult> PauseSession(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();

            session.Status = "paused";
            await _context.SaveChangesAsync();
            return Ok(session);
        }

        [HttpPost("{id}/resume")]
        public async Task<IActionResult> ResumeSession(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();

            session.Status = "active";
            await _context.SaveChangesAsync();
            return Ok(session);
        }

        [HttpPost("{id}/score")]
        public async Task<IActionResult> SubmitScore(string id, [FromBody] PuzzleScoreDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();

            var puzzleResult = new PuzzleResult
            {
                SessionId = id,
                Score = dto.Score,
                CompletionTimeSeconds = dto.CompletionTimeSeconds
            };

            _context.PuzzleResults.Add(puzzleResult);
            await _context.SaveChangesAsync();
            return Ok(puzzleResult);
        }
        [HttpGet("insights")]
        public async Task<IActionResult> GetInsights()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessions = await _context.Sessions
                .Where(s => s.UserId == userId && s.Status == "completed")
                .ToListAsync();

            int totalFocusSeconds = sessions.Sum(s => s.ActualDurationSeconds);
            var totalFocusMinutes = totalFocusSeconds / 60;
            
            var totalSessionsCount = sessions.Count;
            var overallAverageConcentration = totalSessionsCount > 0 ? (int)Math.Round(sessions.Average(s => s.AverageConcentration)) : 0;

            var activeDaysCount = sessions.Select(s => s.CreatedAt.Date).Distinct().Count();
            var averageMinutesPerDay = activeDaysCount > 0 ? (int)Math.Round((double)totalFocusMinutes / activeDaysCount) : 0;

            // Weekly Data (last 7 days)
            var weeklyData = new List<int>();
            int currentWeekSeconds = 0;
            int lastWeekSeconds = 0;

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = DateTime.UtcNow.Date.AddDays(-i);
                var dailySumSeconds = sessions
                    .Where(s => s.CreatedAt.Date == targetDate)
                    .Sum(s => s.ActualDurationSeconds);
                
                weeklyData.Add(dailySumSeconds / 60);
                currentWeekSeconds += dailySumSeconds;
            }

            for (int i = 13; i >= 7; i--)
            {
                var targetDate = DateTime.UtcNow.Date.AddDays(-i);
                var dailySumSeconds = sessions
                    .Where(s => s.CreatedAt.Date == targetDate)
                    .Sum(s => s.ActualDurationSeconds);
                lastWeekSeconds += dailySumSeconds;
            }

            int improvementPercentage = 0;
            if (lastWeekSeconds > 0)
            {
                improvementPercentage = (int)Math.Round(((double)(currentWeekSeconds - lastWeekSeconds) / lastWeekSeconds) * 100);
            }
            else if (currentWeekSeconds > 0)
            {
                improvementPercentage = 100; // 100% improvement from nothing
            }

            // Monthly Data (last 6 months)
            var monthlyData = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = DateTime.UtcNow.Date.AddMonths(-i);
                var monthlySumSeconds = sessions
                    .Where(s => s.CreatedAt.Year == targetMonth.Year && s.CreatedAt.Month == targetMonth.Month)
                    .Sum(s => s.ActualDurationSeconds);
                monthlyData.Add(monthlySumSeconds / 60);
            }

            return Ok(new
            {
                totalFocusSeconds,
                averageMinutesPerDay,
                overallAverageConcentration,
                totalSessionsCount,
                improvementPercentage,
                weeklyData,
                monthlyData
            });
        }
    }
}
