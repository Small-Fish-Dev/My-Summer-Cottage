namespace Sauna;

partial class Player
{
	public static Player Local { get; private set; }

	[HostSync] public Connection Connection { get; set; }
	public ulong SteamID => Connection.SteamId;
	public string Name => Connection.DisplayName;

	public void Setup( Connection connection )
	{
		Connection = connection;
		GameObject.Name = $"{Name} / {SteamID}";
	}
}
