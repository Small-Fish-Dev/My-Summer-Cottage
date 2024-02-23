namespace Sauna;

partial class Player
{
	public static Player Local { get; private set; }

	private Guid _guid;
	[HostSync] public Guid ConnectionID
	{
		get => _guid;
		set
		{
			_guid = value;
			if ( _guid == Connection.Local.Id )
				Local = this;
		}
	}

	public Connection Connection { get; set; }

	public ulong SteamID => Connection.SteamId;
	public string Name => Connection.DisplayName;

	public void SetupConnection( Connection connection )
	{
		ConnectionID = connection.Id;
		Connection = connection;
		GameObject.Name = $"{Name} / {SteamID}";
	}
}
