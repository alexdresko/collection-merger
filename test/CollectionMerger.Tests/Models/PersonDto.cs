namespace CollectionMerger.Tests.Models;

public sealed class PersonDto
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public List<CatDto> Cats { get; set; } = new();
}
