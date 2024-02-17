namespace Sauna;

public struct Marker
{
	private static readonly List<Marker> all = new List<Marker>
	{
		new( "The Sauna", new Vector3( 1040f, -840f, 89f ), Color.Red ),
	};
	public static IReadOnlyList<Marker> All => all;

	public string Name;
	public Vector3 Position;
	public Color Color;
	public Texture Texture;

	public Marker( string name, Vector3 position, Color? color = null, Texture? texture = null )
	{
		Name = name;
		Position = position;
		Color = color ?? Color.White;
		Texture = texture;
	}
}
