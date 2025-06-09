using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection; // Ensure this is present for GetRequiredService
using System; // For Console.WriteLine

public class DummyAuthFilter : IEndpointFilter
{
    private readonly IDictionary<string, User> _activeTokens;

    public DummyAuthFilter(IDictionary<string, User> activeTokens)
    {
        _activeTokens = activeTokens;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        Console.WriteLine($"\n--- DummyAuthFilter Invoked for Path: {httpContext.Request.Path} ---");

        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            Console.WriteLine("Filter: No Bearer token found in Authorization header.");
            return Results.Unauthorized(); // 401 Unauthorized
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        Console.WriteLine($"Filter: Extracted Token: '{token}'");

        if (_activeTokens.TryGetValue(token, out var user))
        {
            // Token is valid!
            httpContext.Items["CurrentUser"] = user;
            Console.WriteLine($"Filter: Token VALID. User '{user.Username}' authenticated.");
            // Proceed to the next filter or the actual endpoint handler
            return await next(context);
        }
        else
        {
            Console.WriteLine($"Filter: Token '{token}' NOT found in active tokens store.");
            Console.WriteLine($"Filter: Current active tokens count: {_activeTokens.Count}");
            // Optional: List all stored tokens to aid debugging
            // foreach(var key in _activeTokens.Keys) { Console.WriteLine($" - Stored: '{key}'"); }
            return Results.Unauthorized(); // 401 Unauthorized
        }
    }
}