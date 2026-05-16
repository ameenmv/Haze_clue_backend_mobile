using HazeClue.Core.Domain.Entities;
using HazeClue.UI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace HazeClue.UI.Controllers.v1
{
    [Authorize]
    [ApiVersion("1.0")]
    public class UsersController : CustomControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly HazeClue.Infrastructure.DbContext.ApplicationDbContext _context;

        public UsersController(UserManager<AppUser> userManager, HazeClue.Infrastructure.DbContext.ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                nickname = user.Nickname,
                phoneNumber = user.PhoneNumber,
                country = user.Country,
                address = user.Address,
                gender = user.Gender,
                onboardingCompleted = user.OnboardingCompleted,
                eligibilityStatus = user.EligibilityStatus
            });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            if (dto.Nickname != null) user.Nickname = dto.Nickname;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.Country != null) user.Country = dto.Country;
            if (dto.Address != null) user.Address = dto.Address;
            
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Profile updated successfully.", fullName = user.FullName });
        }

        [HttpPatch("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var log = new SecurityLog
            {
                UserId = user.Id,
                Event = "Password changed successfully",
                IpAddress = ipAddress
            };
            _context.SecurityLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }
        [HttpGet("me/notification-settings")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var settings = await _context.NotificationSettings.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (settings == null)
            {
                // Return default settings if none exist
                return Ok(new NotificationSetting { UserId = userId! });
            }

            return Ok(settings);
        }

        [HttpPut("me/notification-settings")]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var settings = await _context.NotificationSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new NotificationSetting { UserId = userId! };
                _context.NotificationSettings.Add(settings);
            }

            settings.GeneralNotification = dto.GeneralNotification;
            settings.Sound = dto.Sound;
            settings.Vibrate = dto.Vibrate;
            settings.AppUpdates = dto.AppUpdates;
            settings.ServiceAlerts = dto.ServiceAlerts;
            settings.NewServiceAvailable = dto.NewServiceAvailable;
            settings.NewTipsAvailable = dto.NewTipsAvailable;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification settings updated successfully." });
        }
        [HttpGet("me/device-settings")]
        public async Task<IActionResult> GetDeviceSettings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var settings = await _context.DeviceSettings.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (settings == null)
            {
                // Return default settings if none exist
                return Ok(new DeviceSetting { UserId = userId! });
            }

            return Ok(settings);
        }

        [HttpPut("me/device-settings")]
        public async Task<IActionResult> UpdateDeviceSettings([FromBody] UpdateDeviceSettingsDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var settings = await _context.DeviceSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new DeviceSetting { UserId = userId! };
                _context.DeviceSettings.Add(settings);
            }

            settings.IntensityLevel = dto.IntensityLevel;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Device settings updated successfully." });
        }
    }
}
