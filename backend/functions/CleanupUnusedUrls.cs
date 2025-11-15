using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tinyurl.Data;

namespace TinyUrlCleanup
{
    public class CleanupUnusedUrls
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _context;

        public CleanupUnusedUrls(ILoggerFactory loggerFactory, AppDbContext context)
        {
            _logger = loggerFactory.CreateLogger<CleanupUnusedUrls>();
            _context = context;
        }

        [Function("CleanupUnusedUrls")]
        public async Task Run([TimerTrigger("0 */1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"Cleanup function executed at: {DateTime.Now}");

            var cutoff = DateTime.UtcNow.AddHours(-24);

            var unused = await _context.ShortUrls
                .Where(u => u.Clicks == 0 && u.CreatedAt < cutoff)
                .ToListAsync();

            if (unused.Count != 0)
            {
                _context.ShortUrls.RemoveRange(unused);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {unused.Count} unused URLs.");
            }
            else
            {
                _logger.LogInformation("No unused URLs to delete.");
            }
        }
    }
}
