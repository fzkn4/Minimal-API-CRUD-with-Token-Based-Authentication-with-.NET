using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http; // For StatusCodes and Results
using System.Collections.Generic; // For List<User> and IDictionary
using System.Linq; // For FirstOrDefault and Any
using System; // Required for Guid

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

// --- NEW: In-memory store for active dummy tokens ---
// Key: The dummy token string (Guid.ToString())
// Value: The User object associated with that token
var activeTokens = new Dictionary<string, User>();
builder.Services.AddSingleton<IDictionary<string, User>>(activeTokens); // Register it in DI

// Initial dummy user data (registered as a singleton in DI)
var users = new List<User>();
users.Add(new User(1, "ezio", GenerateSha256Hash("password123"), "Ezio Auditore", "ezio@example.com", "Florence", "System", false));
users.Add(new User(2, "fzkn4", GenerateSha256Hash("securepass"), "Fzkn4 Test", "fzkn4@example.com", "Rome", "System", false));
users.Add(new User(3, "auditore", GenerateSha256Hash("anotherpass"), "Auditore Da Firenze", "auditore@example.com", "Venice", "System", false));
users.Add(new User(99, "admin", GenerateSha256Hash("adminpass"), "Super Admin", "admin@example.com", "Headquarters", "System", true));
builder.Services.AddSingleton<List<User>>(users); // Register the List<User> in DI

var app = builder.Build();

// --- Redirection Middleware ---
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        context.Response.Headers.Location = "/users";
        return;
    }
    await next(context);
});


// --- Login endpoint: Issues tokens for any valid user ---
app.MapPost("/login", (LoginRequest loginRequest, List<User> registeredUsers, IDictionary<string, User> activeTokensStore) =>
{
    // Find the user by username
    var user = registeredUsers.FirstOrDefault(u => u.Username.ToLowerInvariant() == loginRequest.Username.ToLowerInvariant());

    // Check if user exists and password is correct
    if (user == null || GenerateSha256Hash(loginRequest.Password) != user.Password)
    {
        Console.WriteLine($"Login failed for user: {loginRequest.Username} (Invalid credentials)");
        return Results.Unauthorized(); // Invalid credentials
    }

    // If valid, generate a unique dummy token (GUID is good for uniqueness)
    var newToken = Guid.NewGuid().ToString();

    // Store the token and associated user in our in-memory store
    activeTokensStore[newToken] = user;

    Console.WriteLine($"Login successful for {user.Username}! Token: {newToken}. Total active tokens: {activeTokensStore.Count}");
    return Results.Ok(new { Token = newToken, Message = $"Login successful for {user.Username}!" });
});


// We'll apply this filter to all /users CRUD operations to enforce token authentication
var authenticatedApi = app.MapGroup("/users")
    .AddEndpointFilterFactory((factoryContext, next) =>
    {
        return async invocationContext =>
        {
            var tokenStore = invocationContext.HttpContext.RequestServices.GetRequiredService<IDictionary<string, User>>();
            var filter = new DummyAuthFilter(tokenStore);
            return await filter.InvokeAsync(invocationContext, next);
        };
    });


// --- Protected Endpoints (now part of the authenticatedApi group) ---

authenticatedApi.MapGet("/", (List<User> allUsers) => allUsers); // Get all users, but only if authenticated

authenticatedApi.MapPost("/", (User newUser, List<User> allUsers, HttpContext context) =>
{
    // Authorization: Only allow admins to create new users (optional, but common)
    if (!context.Items.TryGetValue("CurrentUser", out var userObject) || userObject is not User currentUser || !currentUser.IsAdmin)
    {
        return Results.BadRequest("Only administrators can create new users.");
    }

    if (allUsers.Any(e => e.Id == newUser.Id))
    {
        return Results.Conflict($"User with an ID: {newUser.Id} already existed.");
    }
    var userWithHashedPassword = newUser with
    {
        Password = GenerateSha256Hash(newUser.Password)
    };
    allUsers.Add(userWithHashedPassword);
    return Results.Created($"/users/{userWithHashedPassword.Id}", userWithHashedPassword);
});

authenticatedApi.MapDelete("/{id}", (int id, List<User> allUsers, HttpContext context) =>
{
    // Authorization: Only allow admins to delete users
    if (!context.Items.TryGetValue("CurrentUser", out var userObject) || userObject is not User currentUser || !currentUser.IsAdmin)
    {
        return Results.BadRequest("Only administrators can delete users.");
    }

    var userToDelete = allUsers.FirstOrDefault(e => e.Id == id);
    if (userToDelete == null)
    {
        return Results.NotFound("This user doesn't exist.");
    }
    else if (userToDelete.IsAdmin)
    {
        return Results.BadRequest("Removing admin is forbidden."); // Still prevents deleting an admin user
    }
    allUsers.Remove(userToDelete);
    return Results.NoContent();
});

authenticatedApi.MapGet("/{id}", (int id, List<User> allUsers) =>
{
    var user = allUsers.FirstOrDefault(e => e.Id == id);
    if (user == null)
    {
        return Results.NotFound("User doesn't exist.");
    }
    return Results.Ok(user);
});

// --- Logout endpoint ---
app.MapPost("/logout", (HttpContext context, IDictionary<string, User> activeTokensStore) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
    {
        Console.WriteLine("Logout: No Bearer token found in header.");
        return Results.Unauthorized();
    }

    var token = authHeader.Substring("Bearer ".Length).Trim();

    if (activeTokensStore.Remove(token))
    {
        Console.WriteLine($"Logout successful for token: {token}. Remaining active tokens: {activeTokensStore.Count}");
        return Results.Ok("Logged out successfully.");
    }
    else
    {
        Console.WriteLine($"Logout failed: Token '{token}' not found in store.");
        return Results.Unauthorized();
    }
});



app.Run();

// Reusable SHA256 Hashing Method
static string GenerateSha256Hash(string inputString)
{
    using (SHA256 sha256Hash = SHA256.Create())
    {
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}

// Record Definitions (must be public as discussed earlier)
public record User(int Id, string Username, string Password, string Fullname, string Email, string Address, string AddedBy, bool IsAdmin);
public record LoginRequest(string Username, string Password);