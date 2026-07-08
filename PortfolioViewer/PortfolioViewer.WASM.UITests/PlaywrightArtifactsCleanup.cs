namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Cleans old Playwright artifacts (screenshots and videos) before each test run.
/// Ensures artifacts from previous runs don't accumulate and clutter debugging.
/// Runs as part of the WebApplicationFactory collection setup.
/// </summary>
public static class PlaywrightArtifactsCleanup
{
    public static void Cleanup()
    {
        var baseDir = Directory.GetCurrentDirectory();

        CleanupDirectory(Path.Combine(baseDir, "playwright-screenshots"));
        CleanupDirectory(Path.Combine(baseDir, "playwright-videos"));
    }

    private static void CleanupDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        try
        {
            var fileCount = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
            if (fileCount > 0)
            {
                Directory.Delete(path, recursive: true);
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception ex)
        {
            // Best-effort: don't fail the test suite over cleanup issues
            Console.WriteLine($"[PlaywrightArtifactsCleanup] Failed to clean '{path}': {ex.Message}");
        }
    }
}
