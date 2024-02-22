using Sandbox;

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


	public bool HasBeenPlayed { get; set; } = false;

	/// <summary>
	/// Are any of the event components inside playing?
	/// </summary>
	public bool IsPlaying => Components.GetAll<EventComponent>()
		.Any( x => x.IsPlaying );

	/// <summary>
	/// If all required event components have finished playing
	/// </summary>
	public bool IsFinished => Components.GetAll<EventComponent>()
		.All( x => (x.Triggered && !x.IsPlaying) || (!x.RequiredToFinish && !x.IsPlaying) || !x.Enabled );


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
		if ( Game.IsEditor ) return;

		foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
		{
			if ( component != this )
				component.Enabled = true; // Make sure to enable back all the components in case they were disabled
		}
	}

	protected override void OnUpdate()
	{
	}
}
