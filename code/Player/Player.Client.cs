namespace Sauna;

partial class Player
{
	public static IReadOnlyList<Player> All => players;
	private static List<Player> players = new List<Player>();

	public static Player Local { get; private set; }

	private Guid _guid;
	[HostSync] public Guid ConnectionID
	{
		get => _guid;
		set
		{
			_guid = value;
			Connection = Networking.FindConnection( _guid );
			if ( !players.Contains( this ) )
				players.Add( this );

			if ( _guid == Connection.Local.Id )
				Local = this;
		}
	}

	public Connection Connection { get; private set; }

	public ulong SteamID => Connection.SteamId;
	public string Name => Connection.DisplayName;

	public void SetupConnection( Connection connection )
	{
		ConnectionID = connection.Id;
		GameObject.Name = $"{Name} / {SteamID}";
	}

	public static Player GetByID( Guid id )
		=> players.FirstOrDefault( x => x.ConnectionID == id );
}
