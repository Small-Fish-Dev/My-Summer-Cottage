namespace Sauna;

public struct Marker
{
	private static List<Marker> all = new List<Marker>
	{
		new( "Home", Vector3.Zero, Color.Red ),
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
