using ServiceHub.Domain.Entities;

namespace ServiceHub.Infrastructure.Identity;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) Generate(ApplicationUser user, IEnumerable<string> roles);
}
