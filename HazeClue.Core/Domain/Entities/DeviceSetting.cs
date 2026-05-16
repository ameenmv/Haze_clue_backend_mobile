using System;

namespace HazeClue.Core.Domain.Entities
{
    public class DeviceSetting
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }
        
        public double IntensityLevel { get; set; } = 0.5; // Default 50%
    }
}
