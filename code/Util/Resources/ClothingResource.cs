namespace Sauna;

[GameResource("Sauna Clothing", "cloth", "Clothing for Sauna!", Icon = "camera_front")]
public class ClothingResource : GameResource
{
	/// <summary>
	/// Dictionary of all the available clothing resources.
	/// </summary>
	public static IReadOnlyDictionary<string, ClothingResource> All => all;

	private static Dictionary<string, ClothingResource> all = new();

	/// <summary>
	/// The name of this clothing.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The path to this clothing's model.
	/// </summary>
	[ResourceType("vmdl")]
	public string Model { get; set; }

	protected override void PostLoad()
	{
		base.PostLoad();

		if (!all.ContainsKey(ResourceName))
			all.Add(ResourceName, this);
	}
}
