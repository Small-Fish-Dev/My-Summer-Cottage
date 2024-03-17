namespace Sauna;

public enum MarkerIcon
{
	Home,
	Sub,
	Primary,
	Event,
	Water,
	Town
}

public class Marker
{
	public static IReadOnlyList<Marker> All => all;
	private static List<Marker> all = new();

	public string Name;
	public Vector3 Position;
	public MarkerIcon Icon;
	public string Texture => Icon switch
	{
		MarkerIcon.Home => "ui/hud/home.png",
		MarkerIcon.Primary => "ui/hud/primary_task.png",
		MarkerIcon.Event => "ui/hud/question_mark.png",
		MarkerIcon.Water => "ui/hud/water_marker.png",
		MarkerIcon.Town => "ui/hud/town_marker.png",
		_ => "ui/hud/substory_task.png",
	};
	public string HexColor => Icon switch
	{
		MarkerIcon.Home => "#FFFFFF",
		MarkerIcon.Primary => "#bc4d2a",
		MarkerIcon.Event => "#FFFFFF",
		MarkerIcon.Water => "#FFFFFF",
		MarkerIcon.Town => "#FFFFFF",
		_ => "#0e599f",
	};

	private Marker( string name, Vector3 position, MarkerIcon icon )
	{
		Name = name;
		Position = position;
		Icon = icon;
		all.Add( this );
	}

	/// <summary>
	/// Creates a new compass marker.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="position"></param>
	/// <param name="icon"></param>
	/// <returns></returns>
	public static Marker Create( string name, Vector3 position, MarkerIcon icon = MarkerIcon.Sub )
	{
		var marker = new Marker( name, position, icon );
		UI.Compass.Instance?.InitializeMarker( marker );
		return marker;
	}

	/// <summary>
	/// Deletes a compass marker.
	/// </summary>
	public void Delete()
	{
		UI.Compass.Instance.RemoveMarker( this );
		all.Remove( this );
	}
}
