using CollectionMerger.Tests.Models.Deletion;
using CollectionMerger.Tests.Models.Nested;

namespace CollectionMerger.Tests;

public class Tests {
    [Test]
    public void MapFrom_MergesNestedCollections_AndProducesReport() {
        var destinationPeople = new List<Person>
        {
            new()
            {
                ID = 1,
                Name = "Person 1 will be updated",
                Cats =
                [
                    new() { ID = 1, Name = "Cat 1 will be updated" },
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

        var report = destinationPeople.MapFrom(
            source: sourcePeople,
            matchPredicate: (srcPerson, destPerson) => srcPerson.ID == destPerson.ID,
            mapProperties: (srcPerson, destPerson, m1) => {
                destPerson.ID = srcPerson.ID;
                destPerson.Name = srcPerson.Name;

                destPerson.Cats.MapFrom(
                    parent: m1,
                    source: srcPerson.Cats,
                    matchPredicate: (srcCat, destCat) => srcCat.ID == destCat.ID,
                    mapProperties: (srcCat, destCat, _m2) => {
                        destCat.ID = srcCat.ID;
                        destCat.Name = srcCat.Name;
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
    public void MapFrom_RemovesDestination_WhenSourceIsMarkedDeleted() {
        var destination = new List<FlaggedDestination>
        {
            new() { ID = 1, Name = "Destination 1" },
            new() { ID = 2, Name = "Destination 2" }
        };

        var source = new List<FlaggedSource>
        {
            new() { ID = 1, Name = "Source 1", Deleted = false },
            new() { ID = 2, Name = "Source 2", Deleted = true }
        };

        var report = destination.MapFrom(
            source: source,
            matchPredicate: (src, dest) => src.ID == dest.ID,
            mapProperties: (src, dest, _m) => {
                dest.ID = src.ID;
                dest.Name = src.Name;
            },
            isSourceDeleted: src => src.Deleted);

        Assert.That(destination.Select(item => item.ID), Is.EquivalentTo(new[] { 1 }));
        Assert.That(report.RemovedCount, Is.EqualTo(1));
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Removed && c.Path == "FlaggedDestination[2]"), Is.True);
    }

    [Test]
    public void MapFrom_UsesDeleteDestinationAction_ForRemovals() {
        var destination = new List<SoftDeleteDestination>
        {
            new() { ID = 1, Name = "Destination 1" }
        };

        var source = new List<SoftDeleteSource>();

        var report = destination.MapFrom(
            source: source,
            matchPredicate: (src, dest) => src.ID == dest.ID,
            mapProperties: (src, dest, _m) => {
                dest.ID = src.ID;
                dest.Name = src.Name;
            },
            deleteDestination: dest => dest.Deleted = true);

        Assert.That(destination.Count, Is.EqualTo(1));
        Assert.That(destination.Single().Deleted, Is.True);
        Assert.That(report.RemovedCount, Is.EqualTo(1));
        Assert.That(report.Changes.Any(c => c.ChangeType == ChangeType.Removed && c.Path == "SoftDeleteDestination[1]"), Is.True);
    }

    [Test]
    public void MapFrom_WithIEnumerableSource_WorksCorrectly()
    {
        var destination = new List<Person>
        {
            new() { ID = 1, Name = "Alice" }
        };

        // Use a true IEnumerable (not IReadOnlyCollection) to hit the IEnumerable overload
        IEnumerable<PersonDto> source = GetPersonDtos();

        var report = destination.MapFrom(
            source: source,
            matchPredicate: (src, dest) => src.ID == dest.ID,
            mapProperties: (src, dest, _m) =>
            {
                dest.ID = src.ID;
                dest.Name = src.Name;
            });

        Assert.That(destination.Count, Is.EqualTo(2));
        Assert.That(report.UpdatedCount, Is.EqualTo(1));
        Assert.That(report.AddedCount, Is.EqualTo(1));
    }

    private static IEnumerable<PersonDto> GetPersonDtos()
    {
        yield return new PersonDto { ID = 1, Name = "Alice Updated" };
        yield return new PersonDto { ID = 2, Name = "Bob" };
    }

    private static IEnumerable<CatDto> GetCatDtos(IEnumerable<CatDto> cats)
    {
        foreach (var cat in cats)
        {
            yield return cat;
        }
    }

    [Test]
    public void MapFrom_NestedWithIEnumerableSource_WorksCorrectly()
    {
        var destination = new List<Person>
        {
            new()
            {
                ID = 1,
                Name = "Person 1",
                Cats = [new() { ID = 1, Name = "Cat 1" }]
            }
        };

        var source = new List<PersonDto>
        {
            new()
            {
                ID = 1,
                Name = "Person 1 Updated",
                Cats = [new() { ID = 1, Name = "Cat 1 Updated" }, new() { ID = 2, Name = "Cat 2" }]
            }
        };

        var report = destination.MapFrom(
            source: source,
            matchPredicate: (src, dest) => src.ID == dest.ID,
            mapProperties: (srcPerson, destPerson, m1) =>
            {
                destPerson.ID = srcPerson.ID;
                destPerson.Name = srcPerson.Name;

                // Use a true IEnumerable (via yield return) to hit the nested IEnumerable overload
                IEnumerable<CatDto> catsAsEnumerable = GetCatDtos(srcPerson.Cats);
                destPerson.Cats.MapFrom(
                    parent: m1,
                    source: catsAsEnumerable,
                    matchPredicate: (srcCat, destCat) => srcCat.ID == destCat.ID,
                    mapProperties: (srcCat, destCat, _m2) =>
                    {
                        destCat.ID = srcCat.ID;
                        destCat.Name = srcCat.Name;
                    });
            });

        Assert.That(destination.Single().Cats.Count, Is.EqualTo(2));
        Assert.That(report.AddedCount, Is.EqualTo(1));
        Assert.That(report.UpdatedCount, Is.EqualTo(2));
    }

    [Test]
    public void MapFrom_IgnoresDeletedItemNotInDestination()
    {
        var destination = new List<FlaggedDestination>
        {
            new() { ID = 1, Name = "Destination 1" }
        };

        var source = new List<FlaggedSource>
        {
            new() { ID = 1, Name = "Source 1", Deleted = false },
            new() { ID = 99, Name = "Non-existent item", Deleted = true }
        };

        var report = destination.MapFrom(
            source: source,
            matchPredicate: (src, dest) => src.ID == dest.ID,
            mapProperties: (src, dest, _m) =>
            {
                dest.ID = src.ID;
                dest.Name = src.Name;
            },
            isSourceDeleted: src => src.Deleted);

        Assert.That(destination.Count, Is.EqualTo(1));
        Assert.That(destination.Single().ID, Is.EqualTo(1));
        Assert.That(report.RemovedCount, Is.EqualTo(0));
    }

    [Test]
    public void MapFrom_DeletesItemFoundInDestination()
    {
        var destination = new List<FlaggedDestination>
        {
            new() { ID = 1, Name = "Item 1" },
            new() { ID = 2, Name = "Item 2" }
        };

        var source = new List<FlaggedSource>
        {
            new() { ID = 1, Name = "Item 1 Updated", Deleted = false },
            new() { ID = 2, Name = "Item 2", Deleted = true } // Deleted AND in destination
        };

        var report = destination.MapFrom(
            source: source,
            matchPredicate: (src, dest) => src.ID == dest.ID,
            mapProperties: (src, dest, _m) =>
            {
                dest.ID = src.ID;
                dest.Name = src.Name;
            },
            isSourceDeleted: src => src.Deleted,
            deleteDestination: dest => dest.IsDeleted = true);

        // Should update item 1 and mark item 2 as deleted
        Assert.That(destination.Count, Is.EqualTo(2));
        Assert.That(destination[0].Name, Is.EqualTo("Item 1 Updated"));
        Assert.That(destination[1].IsDeleted, Is.True);
        Assert.That(report.UpdatedCount, Is.EqualTo(1));
        Assert.That(report.RemovedCount, Is.EqualTo(1));
    }
}
