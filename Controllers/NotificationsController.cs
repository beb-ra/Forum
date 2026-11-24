using Microsoft.AspNetCore.Mvc;
using Forum.Data;
using Microsoft.AspNetCore.Authorization;
using ForumWebAPI.Data.Repositories;
using ForumWebAPI.Model;
using ForumWebAPI.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IRepository<Notification> _notificationRepository;

        public NotificationsController(IRepository<Notification> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.GetUserId();
            var notifications = await _notificationRepository.Get()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreationDate)
                .Take(20)
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(int id)
        {
            var notification = _notificationRepository.Get(id);
            if (notification == null || notification.UserId != User.GetUserId()) return NotFound();

            notification.IsRead = true;
            _notificationRepository.Update(id, notification);
            return NoContent();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            var unread = await _notificationRepository.Get()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                _notificationRepository.Update(n.Id, n);
            }
            return NoContent();
        }
    }
}
