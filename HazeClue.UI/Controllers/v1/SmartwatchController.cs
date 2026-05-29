using HazeClue.Core.Domain.Entities;
using HazeClue.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HazeClue.UI.Controllers.v1
{
    [Authorize]
    [ApiVersion("1.0")]
    public class SmartwatchController : CustomControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SmartwatchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncData([FromBody] SmartwatchDataDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var data = new SmartwatchData
            {
                UserId = userId,
                HeartRate = dto.HeartRate,
                Hrv = dto.Hrv,
                SleepScore = dto.SleepScore,
                Steps = dto.Steps,
                Timestamp = dto.Timestamp ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SmartwatchData.Add(data);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Smartwatch data synced successfully." });
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var recentData = await _context.SmartwatchData
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .Take(7)
                .ToListAsync();

            return Ok(recentData);
        }
    }

    public class SmartwatchDataDto
    {
        public double? HeartRate { get; set; }
        public double? Hrv { get; set; }
        public double? SleepScore { get; set; }
        public int? Steps { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
