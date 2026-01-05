# CollectionMerger

A small library to **synchronize/merge collections** while producing a report of changes.

## Install

```bash
dotnet add package CollectionMerger
```

## Quick start

See the API entry points in `CollectionSyncExtensions` and the report type `SyncReport`.

```csharp
// Example only – adjust to your concrete models.
// var report = target.SyncFrom(source, ...);
// Console.WriteLine(report);
```

## Releasing

This repo is set up to publish to NuGet when you push a tag like `v1.2.3`.

- Bump version + tag + push (PowerShell):
  - `./scripts/Release-Version.ps1 -Version 1.2.3`

### GitHub setup

The release workflow expects a GitHub Actions secret:

- `NUGET_API_KEY`: a NuGet.org API key with permission to push packages.

GitHub Actions will build, test, pack, publish to NuGet, and create a GitHub Release.
