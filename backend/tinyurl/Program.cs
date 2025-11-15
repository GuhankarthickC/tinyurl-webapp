using Microsoft.EntityFrameworkCore;
using tinyurl.Data;
using tinyurl.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on the port Azure provides
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// === SERVICES ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Tiny URL API", Version = "v1" });
});
var app = builder.Build();

// Get BaseUrl from configuration
var baseUrl = app.Configuration["AppSettings:BaseUrl"];

// === MIDDLEWARE ===
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tiny URL API v1");
    c.RoutePrefix = "swagger"; 
});

// === ENDPOINTS ===
app.MapPost("/api/add", async (AppDbContext db, TinyUrlAddDto request) =>
{
    if (!Uri.TryCreate(request.originalURL, UriKind.Absolute, out _))
        return Results.BadRequest(new { error = "Invalid URL" });

    string shortCode;
    do { shortCode = GenerateShortCode(); }
    while (await db.ShortUrls.AnyAsync(u => u.ShortCode == shortCode));

    var url = new ShortUrl
    {
        ShortCode = shortCode,
        OriginalUrl = request.originalURL,
        IsPrivate = request.isPrivate
    };

    db.ShortUrls.Add(url);
    await db.SaveChangesAsync();

    var shortUrl = $"{baseUrl}/{shortCode}";

    return Results.Created($"/{shortCode}", new { shortUrl });
})
.WithName("CreateShortUrl")
.WithTags("Urls")
.Produces<TinyUrl>(201)
.Produces(400);

app.MapGet("/api/public", async (AppDbContext db) =>
{
    var publicUrls = await db.ShortUrls
        .Where(u => !u.IsPrivate)
        .OrderByDescending(u => u.CreatedAt)
        .Select(u => new TinyUrl(
            u.ShortCode,
            $"{baseUrl}/{u.ShortCode}",
            u.OriginalUrl,
            u.Clicks,
            u.IsPrivate
        ))
        .ToListAsync();

    return Results.Ok(publicUrls);
})
.WithName("GetPublicUrls")
.WithTags("Urls")
.Produces<List<TinyUrl>>(200);

app.MapDelete("/api/delete/{code}", async (AppDbContext db, HttpContext context, string code) =>
{
    var token = context.Request.Headers["X-Secret-Token"].FirstOrDefault();
    var configToken = context.RequestServices.GetRequiredService<IConfiguration>()["secretToken"];

    if (token != configToken)
        return Results.Unauthorized();

    var url = await db.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == code);
    
    if (url == null)
        return Results.NotFound(new { error = "URL not found" });

    db.ShortUrls.Remove(url);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "URL deleted successfully" });
})
.WithName("DeleteUrl")
.WithTags("Urls")
.Produces(200)
.Produces(404);

app.MapDelete("/api/delete-all", async (AppDbContext db) =>
{
    var allUrls = await db.ShortUrls.ToListAsync();
    
    if (allUrls.Count == 0)
        return Results.Ok(new { message = "No URLs to delete" });

    db.ShortUrls.RemoveRange(allUrls);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = $"Deleted {allUrls.Count} URLs successfully" });
})
.WithName("DeleteAllUrls")
.WithTags("Urls")
.Produces(200);

app.MapPut("/api/update/{code}", async (AppDbContext db, string code) =>
{
    var url = await db.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == code);
    
    if (url == null)
        return Results.NotFound(new { error = "URL not found" });

    url.Clicks = 0;

    await db.SaveChangesAsync();

    return Results.Ok(new TinyUrl(
        url.ShortCode,
        $"{baseUrl}/{url.ShortCode}",
        url.OriginalUrl,
        url.Clicks,
        url.IsPrivate
    ));
})
.WithName("UpdateUrl")
.WithTags("Urls")
.Produces<TinyUrl>(200)
.Produces(404);

app.MapGet("/{code}", async (AppDbContext db, string code) =>
{
    var url = await db.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == code);
    
    if (url == null)
        return Results.NotFound(new { error = "URL not found" });

    url.Clicks++;
    await db.SaveChangesAsync();

    return Results.Redirect(url.OriginalUrl);
})
.WithName("RedirectToOriginalUrl")
.WithTags("Urls")
.Produces(302)
.Produces(404);


app.Run();

// === HELPER METHODS ===
static string GenerateShortCode()
{
    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    return new string([.. Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)])]);
}

// === MODELS ===
record TinyUrlAddDto(string originalURL, bool isPrivate);
record TinyUrl(string code, string shortURL, string originalURL, int totalClicks, bool isPrivate);


