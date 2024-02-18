namespace Sauna;

public struct Marker
{
	private static readonly List<Marker> all = new List<Marker>
	{
		new( "Sauna", new Vector3( 1040f, -840f, 89f ), "ui/hud/home.png" ),
	};
	public static IReadOnlyList<Marker> All => all;

	public string Name;
	public Vector3 Position;
	public string Texture;

	public Marker( string name, Vector3 position, string texture = null )
	{
		Name = name;
		Position = position;
		Texture = texture;
	}
}
