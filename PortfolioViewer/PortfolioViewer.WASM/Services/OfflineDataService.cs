using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services;

public interface IOfflineDataService
{
    Task SaveCurrentStateAsync();
    Task<bool> HasOfflineDataAsync();
    void StartPeriodicSave(TimeSpan interval);
    void StopPeriodicSave();
}

public class OfflineDataService : IOfflineDataService, IDisposable
{
    private readonly ISqlitePersistenceService _sqlitePersistenceService;
    private readonly ILogger<OfflineDataService> _logger;
    private Timer? _periodicSaveTimer;

    public OfflineDataService(
        ISqlitePersistenceService sqlitePersistenceService,
        ILogger<OfflineDataService> logger)
    {
        _sqlitePersistenceService = sqlitePersistenceService;
        _logger = logger;
    }

    public async Task SaveCurrentStateAsync()
    {
        try
        {
            await _sqlitePersistenceService.SaveToIndexedDBAsync();
            _logger.LogInformation("Current database state saved to IndexedDB");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save current state to IndexedDB");
        }
    }

    public async Task<bool> HasOfflineDataAsync()
    {
        return await _sqlitePersistenceService.HasPersistedDataAsync();
    }

    public void StartPeriodicSave(TimeSpan interval)
    {
        StopPeriodicSave();
        _periodicSaveTimer = new Timer(async _ => await SaveCurrentStateAsync(), 
            null, interval, interval);
        _logger.LogInformation("Started periodic database save with interval: {Interval}", interval);
    }

    public void StopPeriodicSave()
    {
        _periodicSaveTimer?.Dispose();
        _periodicSaveTimer = null;
        _logger.LogInformation("Stopped periodic database save");
    }

    public void Dispose()
    {
        StopPeriodicSave();
    }
}