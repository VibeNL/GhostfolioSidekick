using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services;

public interface ISqlitePersistenceService
{
    Task InitializeDatabaseAsync();
    Task SaveToIndexedDBAsync();
    Task LoadFromIndexedDBAsync();
    Task<bool> HasPersistedDataAsync();
}

public class SqlitePersistenceService : ISqlitePersistenceService
{
    private readonly IDatabasePersistenceService _databasePersistenceService;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly ILogger<SqlitePersistenceService> _logger;
    private const string DatabaseFileName = "portfolio.db";

    public SqlitePersistenceService(
        IDatabasePersistenceService databasePersistenceService,
        IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<SqlitePersistenceService> logger)
    {
        _databasePersistenceService = databasePersistenceService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // Check if we have persisted data in IndexedDB
            if (await HasPersistedDataAsync())
            {
                _logger.LogInformation("Loading database from IndexedDB");
                await LoadFromIndexedDBAsync();
            }

            // Ensure database is created and migrations are applied
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    public async Task SaveToIndexedDBAsync()
    {
        try
        {
            if (File.Exists(DatabaseFileName))
            {
                var databaseData = await File.ReadAllBytesAsync(DatabaseFileName);
                await _databasePersistenceService.SaveDatabaseAsync(databaseData);
                _logger.LogInformation("Database saved to IndexedDB ({Size} bytes)", databaseData.Length);
            }
            else
            {
                _logger.LogWarning("Database file {FileName} does not exist, cannot save to IndexedDB", DatabaseFileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving database to IndexedDB");
            throw;
        }
    }

    public async Task LoadFromIndexedDBAsync()
    {
        try
        {
            var databaseData = await _databasePersistenceService.LoadDatabaseAsync();
            if (databaseData != null && databaseData.Length > 0)
            {
                await File.WriteAllBytesAsync(DatabaseFileName, databaseData);
                _logger.LogInformation("Database loaded from IndexedDB ({Size} bytes)", databaseData.Length);
            }
            else
            {
                _logger.LogInformation("No database data found in IndexedDB");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading database from IndexedDB");
            throw;
        }
    }

    public async Task<bool> HasPersistedDataAsync()
    {
        try
        {
            return await _databasePersistenceService.DatabaseExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for persisted data");
            return false;
        }
    }
}