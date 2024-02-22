using Sandbox;
using Sauna;

public enum EventFrequency
{
	[Icon( "menu_book" )]
	[Description( "Events that are run at specific times in the story. Doesn't get added to the daily events pool" )]
	Story,
	[Icon( "stars" )]
	[Description( "Events that are run once and will never happen again. Can get added to the daily events pool" )]
	Unique,
	[Icon( "timeline" )]
	[Description( "Events that can run multiple times. Can get added to the daily events pool" )]
	Repeatable
}

public enum EventType
{
	[Icon( "not_listed_location" )]
	[Description( "This event will trigger randomly during the day" )]
	Random,
	[Icon( "view_in_ar" )]
	[Description( "This event requires you to show up somewhere to trigger" )]
	Zone,
	[Icon( "touch_app" )]
	[Description( "This event requires you interacting with something to trigger" )]
	Interaction,
	[Icon( "event_busy" )]
	[Description( "This event has an external factor to trigger. (Like Story events)" )]
	Direct
}

public enum EventRarity
{
	[Icon( "grid_on" )]
	Common,
	[Icon( "window" )]
	Uncommon,
	[Icon( "check_box_outline_blank" )]
	Rare,
	[Icon( "grid_off" )]
	None,
}

[Icon( "event" )]
[EditorHandle( "/textures/gizmo/event.png" )]
[Category( "Events" )]
public sealed class EventDefinition : Component, Component.ExecuteInEditor
{
	[Property]
	public string EventName { get; set; }

	[Property]
	public EventType Type { get; set; } = EventType.Random;

	[Property]
	public EventFrequency Frequency { get; set; } = EventFrequency.Repeatable;

	[Property]
	[HideIf( "Frequency", EventFrequency.Story )]
	public EventRarity Rarity { get; set; } = EventRarity.Common;

	[Property]
	[Description( "Can this event trigger when there's another event going on? And can other events trigger whn this one is going on?" )]
	public bool Stackable { get; set; } = true;

	[Property]
	[Description( "Does this event need to be destroyed and recreated for it to restart (Enable if your event has an end state different from the start state)" )]
	public bool ReinstantiateOnRestart { get; set; } = false;

	/// <summary>
	/// Has the event been triggered
	/// </summary>
	public bool HasBeenPlayed { get; set; } = false;

	/// <summary>
	/// Are any of the event components inside playing?
	/// </summary>
	public bool IsPlaying => Components.GetAll<EventComponent>()
		.Any( x => x.IsPlaying );

	/// <summary>
	/// If all required event components have finished playing
	/// </summary>
	public bool IsFinished { get; set; } = false;


	bool _showToggle = false;

	protected override void DrawGizmos()
	{
		// It's done inside of here because this is when we can detect if it's been selected
		if ( Game.IsEditor )
		{
			var shouldShow = GameManager.ActiveScene == GameObject || Gizmo.IsSelected;
			// The components inside are enabled if you're inside of the prefab or you have the prefab selected

			if ( _showToggle != shouldShow )
			{
				foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
				{
					if ( component != this )
						component.Enabled = shouldShow;
				}

				_showToggle = shouldShow;
			}

		}
	}

	protected override void OnStart()
	{
		if ( !GameManager.IsPlaying ) return;

		foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
		{
			if ( component != this )
				component.Enabled = true; // Make sure to enable back all the components in case they were disabled
		}

		foreach ( var eventComponent in Components.GetAll<EventComponent>() )
		{
			foreach ( var trigger in eventComponent.Triggers )
				trigger.OnTrigger += HasBeenTriggered;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !GameManager.IsPlaying ) return;

		var nowFinished = Components.GetAll<EventComponent>()
			.All( x => x.Finished || (!x.RequiredToFinish && !x.IsPlaying) || !x.Enabled );


		if ( !IsFinished && nowFinished )
		{
			GameObject.Enabled = false; // Ok we're done here
										// TODO: For some reason the last event is not setting IsPlaying so we don't reach this to test the reinsantiate
		}

		IsFinished = nowFinished;

	}

	protected override void OnEnabled()
	{
		if ( !GameManager.IsPlaying ) return;

		if ( ReinstantiateOnRestart )
		{

			var allEvents = PrefabLibrary.FindByComponent<EventDefinition>();
			var thisEventPrefab = allEvents.Where( x => x.Name == GameObject.Name ).FirstOrDefault();

			if ( thisEventPrefab != null )
			{
				if ( IsFinished ) // Reset logic
				{
					SceneUtility.GetPrefabScene( thisEventPrefab.Prefab ).Clone( GameObject.Transform.World, name: GameObject.Name );
				}
			}
		}
		else
		{
			foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( component != this )
					component.Enabled = true;
			}

			foreach ( var eventComponent in Components.GetAll<EventComponent>() )
			{
				if ( !eventComponent.Triggered )
					eventComponent.Triggered = false;

				eventComponent.IsPlaying = false;
			}
		}
	}

	protected override void OnDisabled()
	{
		if ( !GameManager.IsPlaying ) return;

		Log.Info( "waaa" );
	}

	protected override void OnDestroy()
	{
		Log.Info( "im dead" );
	}

	void HasBeenTriggered( GameObject _ )
	{
		HasBeenPlayed = true;
	}
}
