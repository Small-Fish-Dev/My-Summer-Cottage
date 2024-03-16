namespace Sauna;

partial class Player
{
	public static IReadOnlyList<Player> All => _internalPlayers;
	public static List<Player> _internalPlayers = new List<Player>();

	public static Player Local { get; private set; }

	private Guid _guid;

	[HostSync]
	public Guid ConnectionID
	{
		get => _guid;
		set
		{
			_guid = value;
			Connection = Connection.Find( _guid );
			
			if ( _guid == Connection.Local.Id )
			{
				Local = this;
				if ( Connection.Local.IsHost )
					Player._internalPlayers.Clear(); 
			}

			if ( !_internalPlayers.Contains( this ) )
				_internalPlayers.Add( this );
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
