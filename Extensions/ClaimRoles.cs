using System.Security.Claims;

namespace ForumWebAPI.Extensions
{
    public static class ClaimRoles
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (id == null) throw new InvalidOperationException("UserId claim not found");
            return int.Parse(id);
        }

        public static bool IsGlobalAdmin(this ClaimsPrincipal user)
        {
            return user.HasClaim("SubforumRole", "global:Admin");
        }

        public static bool IsModeratorOf(this ClaimsPrincipal user, int subforumId)
        {
            return user.HasClaim("SubforumRole", $"{subforumId}:Moderator")
                || user.IsGlobalAdmin();
        }
    }
}
