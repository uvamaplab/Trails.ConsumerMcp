using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.AspNetCore.Authentication;
using Trails.ConsumerMcp.Participants;
using Trails.ConsumerMcp.Tools;

var builder = WebApplication.CreateBuilder(args);
string issuer = builder.Configuration["Clerk:Issuer"]!;        // https://clerk.digital-trails.org
string serverUrl = builder.Configuration["Mcp:PublicUrl"]!;    // https://consumer-mcp.<...>.azurecontainerapps.io/

builder.Services.AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.Authority = issuer;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidateAudience = false,
        };
    })
    .AddMcp(o => o.ResourceMetadata = new()
    {
        AuthorizationServers = { issuer },
        ScopesSupported = ["mcp:tools"],
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(ParticipantContext.ClientName, client =>
    client.BaseAddress = new Uri(builder.Configuration["Trails:ConsumerApi"]!));
builder.Services.AddSingleton<IParticipantContext, ParticipantContext>();

builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<EnrollmentTools>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp().RequireAuthorization();
app.MapGet("/healthz", () => Results.Ok());
app.Run();