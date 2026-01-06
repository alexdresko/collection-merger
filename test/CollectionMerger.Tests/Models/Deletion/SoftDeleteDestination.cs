namespace CollectionMerger.Tests.Models.Deletion;

internal sealed class SoftDeleteDestination
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public bool Deleted { get; set; }
}
