using CollectionMerger.Tests.Models;

namespace CollectionMerger.Tests;

public class Tests
{
    [Test]
    public void MapFrom_MergesNestedCollections_AndProducesReport()
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

        Assert.That(destinationPeople.Select(p => p.ID).Order(), Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That(destinationPeople.Single(p => p.ID == 1).Name, Is.EqualTo("Updated person 1 name"));
        Assert.That(destinationPeople.Single(p => p.ID == 1).Cats.Select(c => c.ID).Order(), Is.EquivalentTo(new[] { 1, 3 }));
        Assert.That(destinationPeople.Single(p => p.ID == 1).Cats.Single(c => c.ID == 1).Name, Is.EqualTo("Updated cat 1 name"));
        Assert.That(destinationPeople.Single(p => p.ID == 2).Cats.Single().ID, Is.EqualTo(4));

        Assert.That(report.TotalChanges, Is.EqualTo(8));
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
}
