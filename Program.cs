using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache(o => o.SizeLimit = 5000);
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddFixedWindowLimiter("api", options => {
        options.PermitLimit = 600;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueLimit = 0;
    });
});
var app = builder.Build();
app.Use(async (context, next) => {
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; script-src 'self'; img-src 'self'; frame-ancestors 'none'";
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
var api = app.MapGroup("/api/games").RequireRateLimiting("api");
api.MapPost("/", (CreateGame request, IMemoryCache cache) => {
    var settings = request.Level switch {
        "easy" => (9, 9, 10), "medium" => (16, 16, 40), "hard" => (30, 16, 99), _ => (0, 0, 0)
    };
    if (settings.Item1 == 0) return Results.BadRequest(new { error = "Nieznany poziom trudności." });
    var game = new Game(settings.Item1, settings.Item2, settings.Item3);
    cache.Set(game.Id, game, new MemoryCacheEntryOptions { Size = 1, SlidingExpiration = TimeSpan.FromHours(2) });
    return Results.Ok(game.View());
});
api.MapPost("/{id:guid}/moves", (Guid id, Move move, IMemoryCache cache) => {
    if (!cache.TryGetValue<Game>(id, out var game) || game is null)
        return Results.NotFound(new { error = "Gra wygasła. Rozpocznij nową partię." });
    lock (game) {
        if (move.Index < 0 || move.Index >= game.Cells.Length || (move.Action != "reveal" && move.Action != "flag"))
            return Results.BadRequest(new { error = "Nieprawidłowy ruch." });
        game.Play(move.Index, move.Action);
        return Results.Ok(game.View());
    }
});
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
record CreateGame(string Level);
record Move(int Index, string Action);

