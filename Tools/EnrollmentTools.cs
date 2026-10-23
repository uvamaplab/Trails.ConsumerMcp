
namespace Trails.ConsumerMcp.Tools
{
    using System.ComponentModel;
    using System.Security.Claims;
    using ModelContextProtocol.Server;
    using Trails.ConsumerMcp.Participants;

    [McpServerToolType]
    public sealed class EnrollmentTools(IParticipantContext participants)
    {
        [McpServerTool(Name = "get_enrollment_status", ReadOnly = true, UseStructuredContent = true)]
        [Description("The caller's enrollment in their current study: status, study code, protocol release, and the dates they enrolled, consented, started, or left. Never includes raw sensor data.")]
        public async Task<EnrollmentStatus> GetEnrollmentStatus(
            ClaimsPrincipal user,
            [Description("Study code, e.g. mtm. Omit to use the caller's active enrollment.")] string? code,
            CancellationToken cancellationToken)
        {
            string pid = participants.PidFor(user);
            return await participants.EnrollmentAsync(pid, code, cancellationToken);
        }
    }
}

