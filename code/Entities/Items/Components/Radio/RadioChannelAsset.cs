namespace Sauna;

[GameResource( "Radio Channel", "radio", "Radio Channel description for Sauna" )]
public class RadioChannelAsset : GameResource
{
	public static IReadOnlyList<RadioChannelAsset> All => all;
	private static List<RadioChannelAsset> all = new();

	public string Name { get; set; }
	public string URL { get; set; }

	protected override void PostLoad()
	{
		base.PostLoad();

		if ( all.FirstOrDefault( c => c.Name == Name ) == null )
			all.Add( this );
	}
}
