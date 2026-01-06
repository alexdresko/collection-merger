using CollectionMerger.Tests.Models;

namespace CollectionMerger.Tests;

public class AsyncTests
{
    [Test]
    public async Task MapFromAsync_MergesNestedCollections_AndProducesReport()
    {
        var destinationPeople = new List<Person>
        {
            new()
            {
                ID = 1,
                Name = "Person 1 will be updated",
                Cats =
                [
                    new() { ID = 1, Name = "Cat 1 be updated" },
                    new() { ID = 2, Name = "Cat 2 will be removed" }
                ]
            },
            new() { ID = 4, Name = "Person 4 will be removed" }
        };

        var sourcePeople = new List<PersonDto>
        {
            new()
            {
                ID = 1,
                Name = "Updated person 1 name",
                Cats =
                [
                    new() { ID = 1, Name = "Updated cat 1 name" },
                    new() { ID = 3, Name = "Added cat 3" }
                ]
            },
            new()
            {
                ID = 2,
                Name = "Person 2 will be added",
                Cats = [new() { ID = 4, Name = "Cat 4 will be added" }]
            },
            new() { ID = 3, Name = "Person 3 will be added" }
        };

        var report = await destinationPeople.MapFromAsync(
            source: sourcePeople,
            matchPredicate: async (srcPerson, destPerson) => 
            {
                await Task.CompletedTask;
                return srcPerson.ID == destPerson.ID;
            },
            mapProperties: async (srcPerson, destPerson, m1) =>
            {
                destPerson.ID = srcPerson.ID;
                destPerson.Name = srcPerson.Name;

                await destPerson.Cats.MapFromAsync(
                    parent: m1,
                    source: srcPerson.Cats,
                    matchPredicate: async (srcCat, destCat) => 
                    {
                        await Task.CompletedTask;
                        return srcCat.ID == destCat.ID;
                    },
                    mapProperties: async (srcCat, destCat, _m2) =>
                    {
                        destCat.ID = srcCat.ID;
                        destCat.Name = srcCat.Name;
                        await Task.CompletedTask;
                    });
            });

        Assert.That(destinationPeople.Select(p => p.ID).Order(), Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That(destinationPeople.Single(p => p.ID == 1).Name, Is.EqualTo("Updated person 1 name"));
        Assert.That(destinationPeople.Single(p => p.ID == 1).Cats.Select(c => c.ID).Order(), Is.EquivalentTo(new[] { 1, 3 }));
        Assert.That(destinationPeople.Single(p => p.ID == 1).Cats.Single(c => c.ID == 1).Name, Is.EqualTo("Updated cat 1 name"));
        Assert.That(destinationPeople.Single(p => p.ID == 2).Cats.Single().ID, Is.EqualTo(4));

        Assert.That(report.TotalChanges, Is.EqualTo(8));
        Assert.That(report.HasChanges, Is.True);
        Assert.That(report.UpdatedCount, Is.EqualTo(2));
        Assert.That(report.AddedCount, Is.EqualTo(4));
        Assert.That(report.RemovedCount, Is.EqualTo(2));

        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Updated && c.Path == "Person[1]"), Is.True);
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Updated && c.Path == "Person[1].Cat[1]"), Is.True);
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Added && c.Path == "Person[2]"), Is.True);
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Added && c.Path == "Person[3]"), Is.True);
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Added && c.Path == "Person[1].Cat[3]"), Is.True);
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Added && c.Path == "Person[2].Cat[4]"), Is.True);
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Removed && c.Path == "Person[4]"), Is.True);
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Removed && c.Path == "Person[1].Cat[2]"), Is.True);
    }

    [Test]
    public async Task MapFromAsync_WithActualAsyncOperations_WorksCorrectly()
    {
        var destination = new List<Person>
        {
            new() { ID = 1, Name = "Alice" }
        };

        var source = new List<PersonDto>
        {
            new() { ID = 1, Name = "Alice Updated" },
            new() { ID = 2, Name = "Bob" }
        };

        var report = await destination.MapFromAsync(
            source: source,
            matchPredicate: async (src, dest) =>
            {
                await Task.Delay(1); // Simulate async operation
                return src.ID == dest.ID;
            },
            mapProperties: async (src, dest, _m) =>
            {
                await Task.Delay(1); // Simulate async operation
                dest.ID = src.ID;
                dest.Name = src.Name;
            });

        Assert.That(destination.Count, Is.EqualTo(2));
        Assert.That(destination.Single(p => p.ID == 1).Name, Is.EqualTo("Alice Updated"));
        Assert.That(destination.Single(p => p.ID == 2).Name, Is.EqualTo("Bob"));
        Assert.That(report.UpdatedCount, Is.EqualTo(1));
        Assert.That(report.AddedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task MapFromAsync_WithIEnumerableSource_WorksCorrectly()
    {
        var destination = new List<Person>
        {
            new() { ID = 1, Name = "Alice" }
        };

        IEnumerable<PersonDto> source = new List<PersonDto>
        {
            new() { ID = 1, Name = "Alice Updated" },
            new() { ID = 2, Name = "Bob" }
        };

        var report = await destination.MapFromAsync(
            source: source,
            matchPredicate: async (src, dest) =>
            {
                await Task.CompletedTask;
                return src.ID == dest.ID;
            },
            mapProperties: async (src, dest, _m) =>
            {
                dest.ID = src.ID;
                dest.Name = src.Name;
                await Task.CompletedTask;
            });

        Assert.That(destination.Count, Is.EqualTo(2));
        Assert.That(report.UpdatedCount, Is.EqualTo(1));
        Assert.That(report.AddedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task MapFromAsync_PropertyChanges_AreDetected()
    {
        var destination = new List<Person>
        {
            new() { ID = 1, Name = "Alice" }
        };

        var source = new List<PersonDto>
        {
            new() { ID = 1, Name = "Alice Updated" }
        };

        var report = await destination.MapFromAsync(
            source: source,
            matchPredicate: async (src, dest) =>
            {
                await Task.CompletedTask;
                return src.ID == dest.ID;
            },
            mapProperties: async (src, dest, _m) =>
            {
                dest.ID = src.ID;
                dest.Name = src.Name;
                await Task.CompletedTask;
            });

        Assert.That(report.UpdatedCount, Is.EqualTo(1));
        var updateChange = report.Changes.Single(c => c.ChangeType == ChangeType.Updated);
        Assert.That(updateChange.PropertyChanges, Is.Not.Null);
        Assert.That(updateChange.PropertyChanges!.Count, Is.EqualTo(1));
        Assert.That(updateChange.PropertyChanges.Single().PropertyName, Is.EqualTo("Name"));
        Assert.That(updateChange.PropertyChanges.Single().OldValue, Is.EqualTo("Alice"));
        Assert.That(updateChange.PropertyChanges.Single().NewValue, Is.EqualTo("Alice Updated"));
    }

    [Test]
    public async Task MapFromAsync_RemovesItemsNotInSource()
    {
        var destination = new List<Person>
        {
            new() { ID = 1, Name = "Alice" },
            new() { ID = 2, Name = "Bob" }
        };

        var source = new List<PersonDto>
        {
            new() { ID = 1, Name = "Alice" }
        };

        var report = await destination.MapFromAsync(
            source: source,
            matchPredicate: async (src, dest) =>
            {
                await Task.CompletedTask;
                return src.ID == dest.ID;
            },
            mapProperties: async (src, dest, _m) =>
            {
                dest.ID = src.ID;
                dest.Name = src.Name;
                await Task.CompletedTask;
            });

        Assert.That(destination.Count, Is.EqualTo(1));
        Assert.That(destination.Single().ID, Is.EqualTo(1));
        Assert.That(report.RemovedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task MapFromAsync_AddsNewItems()
    {
        var destination = new List<Person>();

        var source = new List<PersonDto>
        {
            new() { ID = 1, Name = "Alice" },
            new() { ID = 2, Name = "Bob" }
        };

        var report = await destination.MapFromAsync(
            source: source,
            matchPredicate: async (src, dest) =>
            {
                await Task.CompletedTask;
                return src.ID == dest.ID;
            },
            mapProperties: async (src, dest, _m) =>
            {
                dest.ID = src.ID;
                dest.Name = src.Name;
                await Task.CompletedTask;
            });

        Assert.That(destination.Count, Is.EqualTo(2));
        Assert.That(destination.Select(p => p.ID).Order(), Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(report.AddedCount, Is.EqualTo(2));
    }
}
