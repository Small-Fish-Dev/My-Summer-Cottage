namespace Sauna;

public struct Marker
{
	private static List<Marker> all = new List<Marker>
	{
		new() { Name = "Piss", Position = 0 },
	};
	public static IReadOnlyList<Marker> All => all;

	public string Name;
	public Vector3 Position;
	public Texture Texture;
}
