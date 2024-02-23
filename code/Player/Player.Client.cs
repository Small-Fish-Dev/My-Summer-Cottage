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

	[HostSync] public ulong SteamID { get; set; }
	[HostSync] public string Name { get; set; }

	public void SetupConnection( Connection connection )
	{
		Connection = connection;
		ConnectionID = connection.Id;
		Name = connection.DisplayName;
		SteamID = connection.SteamId;
		GameObject.Name = $"{Name} / {SteamID}";
	}
}
