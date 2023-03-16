namespace Sauna;

public static class SaunaEvent
{
	/// <summary>
	/// Ran everytime a player spawns. 
	/// Parameters: Player pawn
	/// </summary>
	public class OnSpawn : EventAttribute 
	{ 
		public OnSpawn() : base( nameof( SaunaEvent.OnSpawn ) ) { }
	}

	/// <summary>
	/// Ran on player pawn simulate.
	/// Parameters: IClient cl
	/// </summary>
	public class Simulate : EventAttribute
	{
		public Simulate() : base( nameof( SaunaEvent.Simulate ) ) { }
	}

	/// <summary>
	/// Ran on frame simulate.
	/// Parameters: IClient cl
	/// </summary>
	public class FrameSimulate : EventAttribute
	{
		public FrameSimulate() : base( nameof( SaunaEvent.FrameSimulate ) ) { }
	}
}
