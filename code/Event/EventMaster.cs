﻿using static Sauna.Event.EventTrigger;

namespace Sauna.Event;

[Icon( "free_cancellation" )]
public class EventMaster : Component
{
	[Property]
	public List<EventDefinition> CurrentEvents { get; set; }

	/// <summary>
	/// Get how many events the player has triggered so far
	/// </summary>
	public static float CurrentEventsTriggered
	{
		get
		{
			return 1;
		}
	}

	public class EventCompletion
	{
		[JsonInclude]
		public string Event { get; set; }
		[JsonInclude]
		public int TimesTriggered { get; set; }
		[JsonInclude]
		public int TimesCompleted { get; set; }

		public EventCompletion( string @event, int timesTriggered = 0, int timesCompleted = 0 )
		{
			Event = @event; // Don't bug me on this ye?
			TimesTriggered = timesTriggered;
			TimesCompleted = timesCompleted;
		}
	}

	public struct SaunaEventProgress
	{
		[JsonInclude]
		public List<EventCompletion> Events = new();

		public SaunaEventProgress() { }
	}

	public SaunaEventProgress EventsProgression { get; private set; } = new();

	protected override void OnStart()
	{
		LoadEventsProgression();
	}

	internal void AddEventProgression( string eventName, int timesTriggered = 0, int timesCompleted = 0 )
	{
		var newTaskCompletion = new EventCompletion( eventName, timesTriggered, timesCompleted );
		EventsProgression.Events.Add( newTaskCompletion );
	}

	internal EventCompletion InternalGetEventProgression( string eventName )
	{
		var eventCompletionExists = EventsProgression.Events.Any( x => x.Event == eventName );

		if ( eventCompletionExists )
		{
			var foundEventCompletion = EventsProgression.Events.Where( x => x.Event == eventName ).First();
			foundEventCompletion.TimesTriggered++;

			return foundEventCompletion;
		}
		else
		{
			var newEventCompletion = new EventCompletion( eventName, 0, 0 );
			EventsProgression.Events.Add( newEventCompletion );

			return newEventCompletion;
		}
	}

	/// <summary>
	/// Get the current stats on that event
	/// </summary>
	/// <param name="eventName"></param>
	/// <returns></returns>
	public static EventCompletion GetEventCompletion( string eventName )
	{
		var eventMaster = GameManager.ActiveScene.GetAllComponents<EventMaster>().FirstOrDefault(); // Find the event master

		if ( eventMaster == null ) return null;

		return eventMaster.InternalGetEventProgression( eventName );
	}

	internal void InternalEventTriggered( string eventName )
	{
		var eventCompletionExists = EventsProgression.Events.Any( x => x.Event == eventName );

		if ( eventCompletionExists )
		{
			var foundEventCompletion = EventsProgression.Events.Where( x => x.Event == eventName ).First();
			foundEventCompletion.TimesTriggered++;
		}
		else
		{
			AddEventProgression( eventName, 1, 0 );
		}
	}

	/// <summary>
	/// Increase that events's total triggered amount
	/// </summary>
	/// <param name="eventName"></param>
	public static void EventTriggered( string eventName ) // TODO Hook this up
	{
		var eventMaster = GameManager.ActiveScene.GetAllComponents<EventMaster>().FirstOrDefault(); // Find the event master

		if ( eventMaster == null ) return;

		eventMaster.InternalEventTriggered( eventName );
	}

	internal void InternalEventCompleted( string eventName )
	{
		var eventCompletionExists = EventsProgression.Events.Any( x => x.Event == eventName );

		if ( eventCompletionExists )
		{
			var foundEventCompletion = EventsProgression.Events.Where( x => x.Event == eventName ).First();
			foundEventCompletion.TimesCompleted++;
		}
		else
		{
			AddEventProgression( eventName, 0, 1 );
		}

	}

	/// <summary>
	/// Increase that event's total completed amount
	/// </summary>
	/// <param name="eventName"></param>
	public static void TaskCompleted( string eventName ) // TODO Hook this up
	{
		var eventMaster = GameManager.ActiveScene.GetAllComponents<EventMaster>().FirstOrDefault(); // Find the event master

		if ( eventMaster == null ) return;

		eventMaster.InternalEventCompleted( eventName );
	}

	public void LoadEventsProgression()
	{
		if ( FileSystem.Data.FileExists( "events.json" ) )
			EventsProgression = FileSystem.Data.ReadJsonOrDefault<SaunaEventProgress>( "events.json" );
		else
		{
			EventsProgression = new();

			var allEvents = Scene.Components.GetAll<EventDefinition>()
				.DistinctBy( x => x.EventName ); // Avoid multiple event definitions

			foreach ( var @event in allEvents )
				AddEventProgression( @event.EventName );

			InternalSaveEventsProgression();
		}
	}

	internal void InternalSaveEventsProgression()
	{
		var allEvents = Scene.Components.GetAll<EventDefinition>()
				.DistinctBy( x => x.EventName ); // Avoid multiple event definitions

		// If future updates contain new events or we're live adding newer ones, save those to the file too
		foreach ( var @event in allEvents )
			if ( !EventsProgression.Events.Any( x => x.Event == @event.EventName ) )
				AddEventProgression( @event.EventName );

		FileSystem.Data.WriteJson( "events.json", EventsProgression );
	}

	/// <summary>
	/// Save the events current triggered and completion progress/amount
	/// </summary>
	public static void SaveEventsProgression()
	{
		var eventMaster = GameManager.ActiveScene.GetAllComponents<EventMaster>().FirstOrDefault(); // Find the event master

		if ( eventMaster == null ) return;

		eventMaster.InternalSaveEventsProgression();
	}

	protected override void OnFixedUpdate()
	{
		InvokePolledMethods();
	}

	void InvokePolledMethods()
	{
		// Get all the components that are active
		var allEvents = Scene.GetAllComponents<EventTrigger>()
			?.Where( x => x.Active )
			?.Where( x => x.IsPolled )
			?.OrderBy( x => x.LastPoll );

		if ( !allEvents.Any() ) return; // Bail if we have no triggers

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
	public static void InteractionInvoked( string interaction, Guid target, Guid player )
	{
		var allTriggers = GameManager.ActiveScene.GetAllComponents<EventInteractionTrigger>();
		var foundTarget = GameManager.ActiveScene.GetAllObjects( true )
			.Where( x => x.Id == target )
			.FirstOrDefault();
		var foundPlayer = GameManager.ActiveScene.GetAllObjects( true )
			.Where( x => x.Id == player )
			.FirstOrDefault();

		foreach ( var trigger in allTriggers )
		{
			if ( trigger.InteractionIdentifier == interaction )
			{
				if ( !trigger.InsideArea )
					trigger.CallTrigger( foundPlayer, foundTarget );
				else
				{
					var expandedWorldBox = trigger.WorldBBox.Grow( 10f ); // Expand it a bit to make sure it's included even when on the edges

					if ( expandedWorldBox.Contains( foundTarget.Transform.Position ) )
						trigger.CallTrigger( foundPlayer, foundTarget );
				}
			}
		}

		var playerComponent = foundPlayer.Components.Get<Player>();

		TaskMaster.SubmitTriggerSignal( interaction, playerComponent );
	}
}
