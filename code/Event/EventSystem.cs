using static Sauna.Event.EventTrigger;

namespace Sauna.Event;

public class EventSystem : GameObjectSystem
{
	public EventSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.PhysicsStep, 1, InvokePolledMethods, "PolledMethods" );
	}

	void InvokePolledMethods()
	{
		// Get all the components that are active
		var allEvents = Scene.GetAllComponents<EventTrigger>()
			?.Where( x => x.Active )
			?.Where( x => x.IsPolled )
			?.OrderBy( x => x.LastPoll );

		if ( !allEvents.Any() ) return; // Bail if we have no events

		var firstEvent = allEvents.First();
		firstEvent.PolledMethod();
		firstEvent.LastPoll = 0;

		foreach ( var polledEvent in allEvents )
			polledEvent.LastPoll++;

		// Get all events that went over their max poll rate
		var eventsOverMaxPolling = allEvents.Where( x => x.LastPoll >= x.MaxPollingRate );

		foreach ( var expiredEvent in eventsOverMaxPolling )
		{
			expiredEvent.PolledMethod();
			expiredEvent.LastPoll = 0;
		}
	}

	[Broadcast]
	public static void InteractionInvoked( string interaction )
	{
		Log.Info( interaction );
	}
}
