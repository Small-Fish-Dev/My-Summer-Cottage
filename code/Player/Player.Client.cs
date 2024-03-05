namespace Sauna;

partial class Player
{
	public static IReadOnlyList<Player> All => _internalPlayers;
	public static List<Player> _internalPlayers = new List<Player>();

	public static Player Local { get; private set; }
	public static SaunaTask PinnedTask { get; set; }

	private Guid _guid;
	[HostSync]
	public Guid ConnectionID
	{
		get => _guid;
		set
		{
			_guid = value;
			Connection = Networking.FindConnection( _guid );
			if ( !_internalPlayers.Contains( this ) )
				_internalPlayers.Add( this );

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
		=> _internalPlayers.FirstOrDefault( x => x.ConnectionID == id );
}
