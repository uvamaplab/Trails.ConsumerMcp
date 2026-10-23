using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol;

namespace Trails.ConsumerMcp.Participants;

public sealed class ParticipantContext(IHttpClientFactory httpClients, IHttpContextAccessor httpContext, IConfiguration configuration) : IParticipantContext
{
    public const string ClientName = "consumer";

    public string PidFor(ClaimsPrincipal? user)
    {
        string? pid = user?.FindFirstValue("oid") ?? user?.FindFirstValue("sub") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

        return string.IsNullOrWhiteSpace(pid) ? throw new McpException("The caller is not authenticated.") : pid;
    }

    public async Task<EnrollmentStatus> EnrollmentAsync(string pid, string? code, CancellationToken cancellationToken)
    {
        string path = string.IsNullOrWhiteSpace(code)
            ? "onboarding-state"
            : $"onboarding-state?code={Uri.EscapeDataString(code.Trim())}";

        using JsonDocument state = await GetAsync(path, cancellationToken);

        return EnrollmentStatus.From(state.RootElement);
    }

    private async Task<JsonDocument> GetAsync(string path, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CallerToken());
        request.Headers.Add("x-debug", configuration.GetValue<bool>("Trails:Debug") ? "True" : "False");

        using HttpResponseMessage response = await httpClients.CreateClient(ClientName).SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new McpException("The study service rejected the caller's credentials.");

        if (!response.IsSuccessStatusCode)
            throw new McpException($"The study service returned {(int)response.StatusCode} for {path}.");

        await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
    }

    private string CallerToken()
    {
        string? header = httpContext.HttpContext?.Request.Headers.Authorization.FirstOrDefault();

        if (header is not null && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return header["Bearer ".Length..].Trim();

        throw new McpException("The caller is not authenticated.");
    }
}
