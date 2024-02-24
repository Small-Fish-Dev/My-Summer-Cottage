using Sandbox;
using Sandbox.Utility;
using Sauna;

public static partial class SaunaActionNodes
{
	/// <summary>
	/// The event has finished playing.
	/// </summary>
	[ActionGraphNode( "event.finished" ), Pure]
	[Title( "Finish Event" ), Group( "Events" ), Icon( "flash_off" )]
	public static void EventFinished( EventComponent component )
	{
		component.Finish();
	}
}
