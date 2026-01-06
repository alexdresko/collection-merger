namespace CollectionMerger.Tests.Models.Nested;

public sealed class Person
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public List<Cat> Cats { get; set; } = new();
}
