using System.Security.Claims;

namespace Trails.ConsumerMcp.Participants;

public interface IParticipantContext
{
    string PidFor(ClaimsPrincipal? user);

    Task<EnrollmentStatus> EnrollmentAsync(string pid, string? code, CancellationToken cancellationToken);
}
