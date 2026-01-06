namespace CollectionMerger.Tests.Models.Deletion;

internal sealed class FlaggedSource
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public bool Deleted { get; set; }
}
