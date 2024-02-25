using Sandbox;
using Sandbox.Utility;
using Sauna;
using Sauna.Event;

public static partial class SaunaActionNodes
{
	/// <summary>
	/// The event has finished playing.
	/// </summary>
	[ActionGraphNode( "event.finished" )]
	[Title( "Finish Event" ), Group( "Events" ), Icon( "flash_off" )]
	public static void EventFinished( EventComponent component )
	{
		component.Finish();
	}

	/// <summary>
	/// Reset a trigger, for examples if players are already inside and have triggered it already, it will trigger again
	/// </summary>
	[ActionGraphNode( "event.resettrigger" )]
	[Title( "Reset Trigger" ), Group( "Events" ), Icon( "backspace" )]
	public static void ResetTrigger( EventTrigger trigger )
	{
		trigger.Reset();
	}
}
