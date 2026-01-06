# CollectionMerger

[![NuGet](https://img.shields.io/nuget/v/CollectionMerger.svg)](https://www.nuget.org/packages/CollectionMerger/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CollectionMerger.svg)](https://www.nuget.org/packages/CollectionMerger/)
[![CI](https://github.com/alexdresko/collection-merger/actions/workflows/netcore.yml/badge.svg)](https://github.com/alexdresko/collection-merger/actions/workflows/netcore.yml)

Synchronize/merge collections while generating a **change report** (added/updated/removed), including **nested collection** merges.

- Targets: `net8.0`, `net9.0`, `net10.0`
- Main entry point: `CollectionSyncExtensions.MapFrom(...)`
- Output: `SyncReport` with a list of `ChangeRecord` items

See [CHANGELOG.md](CHANGELOG.md) for release history.

## Installation

### .NET CLI

```bash
dotnet add package CollectionMerger
```

### Package Manager

```powershell
Install-Package CollectionMerger
```

### PackageReference

```xml
<PackageReference Include="CollectionMerger" Version="x.y.z" />
```

## Getting started

You merge a source collection into a destination collection by providing:

- `matchPredicate`: how to match a source item to an existing destination item (usually by an ID)
- `mapProperties`: how to copy/update properties from source to destination
- `isSourceDeleted` (optional): treat a source item as deleted even when it exists in the source collection
- `deleteDestination` (optional): custom delete action for destination items (default removes from the collection)

```csharp
using CollectionMerger;

var destination = new List<Person>
{
    new() { Id = 1, Name = "Alice" },
    new() { Id = 2, Name = "Bob" }
};

var source = new List<PersonDto>
{
    new() { Id = 1, Name = "Alice Updated" },
    new() { Id = 3, Name = "Charlie" }
};

var report = destination.MapFrom(
    source: source,
    matchPredicate: (src, dest) => src.Id == dest.Id,
    mapProperties: (src, dest, _m) =>
    {
        dest.Id = src.Id;
        dest.Name = src.Name;
    });

Console.WriteLine($"Added: {report.AddedCount}, Updated: {report.UpdatedCount}, Removed: {report.RemovedCount}");
```

## Examples

### Nested collections (people + cats)

For nested collections, call `MapFrom(...)` on the child collection and pass the parent `Mapper` so paths get nested.

```csharp
using CollectionMerger;

var report = destinationPeople.MapFrom(
    source: sourcePeople,
    matchPredicate: (srcPerson, destPerson) => srcPerson.ID == destPerson.ID,
    mapProperties: (srcPerson, destPerson, m1) =>
    {
        destPerson.ID = srcPerson.ID;
        destPerson.Name = srcPerson.Name;

        destPerson.Cats.MapFrom(
            parent: m1,
            source: srcPerson.Cats,
            matchPredicate: (srcCat, destCat) => srcCat.ID == destCat.ID,
            mapProperties: (srcCat, destCat, _m2) =>
            {
                destCat.ID = srcCat.ID;
                destCat.Name = srcCat.Name;
            });
    });
```

### Inspecting changes

```csharp
foreach (var change in report.Changes)
{
    Console.WriteLine($"{change.ChangeType}: {change.Path}");

    if (change.PropertyChanges is null)
        continue;

    foreach (var prop in change.PropertyChanges)
        Console.WriteLine($"  - {prop.PropertyName}: '{prop.OldValue}' -> '{prop.NewValue}'");
}
```

### Source-driven deletes + custom delete actions

Mark a source item as deleted and remove its destination match (default removal):

```csharp
var report = destination.MapFrom(
    source: source,
    matchPredicate: (src, dest) => src.ID == dest.ID,
    mapProperties: (src, dest, _m) =>
    {
        dest.ID = src.ID;
        dest.Name = src.Name;
    },
    isSourceDeleted: src => src.Deleted);
```

Provide a custom delete action (soft delete instead of removing):

```csharp
var report = destination.MapFrom(
    source: source,
    matchPredicate: (src, dest) => src.ID == dest.ID,
    mapProperties: (src, dest, _m) =>
    {
        dest.ID = src.ID;
        dest.Name = src.Name;
    },
    deleteDestination: dest => dest.Deleted = true);
```

## What the report contains

`SyncReport.Changes` contains `ChangeRecord` entries:

- `ChangeType`: `Added`, `Updated`, or `Removed`
- `Path`: a stable-ish path for the item (supports nesting)
- `Item`: the destination item instance
- `PropertyChanges`: only present for `Updated`

## FAQ

### How are item paths created?

Paths look like `Person[1]` and `Person[1].Cat[3]`.

The `[...]` value is chosen by looking for a public readable `ID` or `Id` property on either the source or destination item. If neither exists, it becomes `?`.

### What counts as an update?

After your `mapProperties` delegate runs, CollectionMerger snapshots public instance scalar properties (excluding enumerables except `string`) and records an `Updated` change if any of those values differ.

### Are collections compared automatically?

No. Collection properties are ignored for property change detection.
If you want nested changes, perform nested merges with the nested overload of `MapFrom(...)`.

### What are the requirements?

- Destination must be an `ICollection<TDestination>`
- `TDestination` must have a parameterless constructor (`new()` constraint)
- Matching behavior is entirely defined by your `matchPredicate` (make sure it uniquely identifies items)

## Feedback / issues

If you hit a bug or want to request a feature, please open an issue:
https://github.com/alexdresko/collection-merger/issues

## Development (this repo)

### Releasing

Releases are automated via Release Please.

- Merge changes into `main` using Conventional Commits.
- Release Please will open/maintain a release PR updating `CHANGELOG.md` and package version.
- Merging the release PR creates the GitHub Release + publishes to NuGet.

### GitHub setup

The release workflow expects a GitHub Actions secret:

- `NUGET_API_KEY` (or `NUGET_TOKEN`): a NuGet.org API key with permission to push packages.
