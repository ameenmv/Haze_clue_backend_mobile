using System;

namespace HazeClue.Core.Domain.Entities
{
    public class SmartwatchData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }
        
        // Smartwatch Metrics
        public double? HeartRate { get; set; }
        public double? Hrv { get; set; }
        public double? SleepScore { get; set; }
        public int? Steps { get; set; }
        
        // Time period the data corresponds to
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
