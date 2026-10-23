using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Trails.ConsumerMcp.Participants;

public sealed record EnrollmentStatus(
    [property: Description("NotEnrolled, Enrolled, Consented, Active, StartFailed, Unenrolled or Removed.")] string Status,
    [property: Description("The study code the participant joined with.")] string? Code,
    [property: Description("The study's id.")] string? Study,
    [property: Description("The study environment, e.g. prod or test.")] string? Env,
    [property: Description("The protocol release the participant's app downloaded.")] string? ProtocolTag,
    DateTimeOffset? EnrolledAt,
    DateTimeOffset? ConsentedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ErroredAt,
    DateTimeOffset? UnenrolledAt,
    DateTimeOffset? RemovedAt)
{
    public static EnrollmentStatus From(JsonElement state)
    {
        string? code = Text(state, "code");
        DateTimeOffset? enrolledAt = When(state, "enrolledAt");
        DateTimeOffset? consentedAt = When(state, "consentedAt");
        DateTimeOffset? startedAt = When(state, "startedAt");
        DateTimeOffset? erroredAt = When(state, "erroredAt");
        DateTimeOffset? unenrolledAt = When(state, "unenrolledAt");
        DateTimeOffset? removedAt = When(state, "removedAt");

        string status = removedAt is not null ? "Removed"
            : code is null ? "NotEnrolled"
            : unenrolledAt is not null ? "Unenrolled"
            : erroredAt is not null ? "StartFailed"
            : startedAt is not null ? "Active"
            : consentedAt is not null ? "Consented"
            : "Enrolled";

        return new EnrollmentStatus(
            status,
            code,
            Text(state, "study"),
            Text(state, "env"),
            Text(state, "downloadedTag"),
            enrolledAt,
            consentedAt,
            startedAt,
            erroredAt,
            unenrolledAt,
            removedAt);
    }

    private static string? Text(JsonElement state, string name) =>
        state.ValueKind == JsonValueKind.Object && state.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset? When(JsonElement state, string name) =>
        DateTimeOffset.TryParse(Text(state, name), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset at)
            ? at
            : null;
}
