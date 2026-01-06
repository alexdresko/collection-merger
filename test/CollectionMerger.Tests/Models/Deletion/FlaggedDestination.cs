namespace CollectionMerger.Tests.Models.Deletion;

internal sealed class FlaggedDestination {
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public bool IsDeleted { get; set; }
}
