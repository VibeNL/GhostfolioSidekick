# Version Management

This project implements automatic version stamping and update detection to ensure users always run the latest version of the application.

## How It Works

### 1. Version Stamping

Versions are automatically stamped during build using MSBuild properties in `Directory.Build.props`:

- **Format**: `{VersionPrefix}-{VersionSuffix}`
- **VersionPrefix**: `1.0.0` (manually updated for major releases)
- **VersionSuffix**: Auto-generated timestamp in UTC format `yyyyMMdd-HHmmss`
- **Example**: `1.0.0-20240315-143022`

### 2. Version Storage

The version is embedded in the assembly's `InformationalVersion` attribute and accessible via:

```csharp
using GhostfolioSidekick.PortfolioViewer.Common;
var version = VersionInfo.Version;
```

### 3. Version API Endpoint

The API service exposes the server version at:

**GET** `/api/version`

Response:
```json
{
	"version": "1.0.0-20240315-143022"
}
```

### 4. Client-Side Version Checking

The WASM client includes `IVersionService` that:
- Provides the client version
- Fetches the server version
- Compares versions to detect updates

**Service Registration** (in `PortfolioViewer.WASM/Program.cs`):
```csharp
builder.Services.AddScoped<IVersionService, VersionService>();
```

**Usage**:
```csharp
@inject IVersionService VersionService

var clientVersion = VersionService.ClientVersion;
var serverVersion = await VersionService.GetServerVersionAsync();
var needsUpdate = await VersionService.IsUpdateAvailableAsync();
```

### 5. Update Detection & Notification

#### Home Page Display
The home page (`Pages/Home.razor`) displays:
- Current client version
- Server version (when available)
- Warning banner if update is available
- "Refresh Now" button to reload the app

#### Automatic Checking
- Version check runs on page load
- Re-checks every 5 minutes
- Service worker notifies on new version activation

#### Service Worker Integration
The published service worker (`service-worker.published.js`):
- Caches assets by version
- Clears old caches on activation
- Supports `SKIP_WAITING` message for immediate updates
- Notifies clients when new version is activated

## Docker Builds

### Build Arguments

The Dockerfile accepts a `VERSION` build argument:

```bash
docker build --build-arg VERSION=1.0.0-production .
```

If not provided, it defaults to a timestamp: `yyyyMMdd-HHmmss`

### CI/CD Integration

The GitHub workflow (`.github/workflows/docker-publish.yml`) automatically:
1. Generates a version from Git SHA or tag
2. Passes it to Docker build via `--build-arg VERSION=...`
3. All built artifacts (API, WASM, Sidekick) share the same version

**Version Generation Logic**:
- **Tagged builds**: Uses the tag name (e.g., `v1.0.0`)
- **Branch builds**: Uses `sha-{commit-sha}` (e.g., `sha-abc123def`)

## Local Development

During local development:
- Versions are stamped with current UTC timestamp
- Service worker is disabled in development mode (`service-worker.js`)
- Version checks still work against the API service

## Updating the Version Prefix

To bump the major/minor version, edit `Directory.Build.props`:

```xml
<VersionPrefix>2.0.0</VersionPrefix>
```

This affects all subsequent builds.

## Testing

The version system ensures:
1. ✅ Client and server versions are tracked separately
2. ✅ Version mismatches are detected automatically
3. ✅ Users are prompted to refresh when updates are available
4. ✅ Service worker clears old caches on version change
5. ✅ Docker builds embed consistent versions across all components

## Troubleshooting

### Version Not Updating
- Clear browser cache and hard refresh (Ctrl+Shift+R)
- Unregister service worker in DevTools → Application → Service Workers
- Check that build includes `/p:VersionSuffix=...` parameter

### Version Mismatch False Positives
- Ensure API and WASM are built from the same commit
- Verify both projects reference `PortfolioViewer.Common` correctly
- Check that `Directory.Build.props` is applied to both projects

### Docker Version Issues
- Verify `VERSION` build arg is passed correctly
- Check all publish stages use the same `$VERSION_SUFFIX`
- Ensure ARG declarations are in the correct stages
