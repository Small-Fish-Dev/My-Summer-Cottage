using Sandbox;
using Sauna;

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

	/// <summary>
	/// Does this event get added to the daily event pool to enable during gameplay?
	/// </summary>
	[Property]
	public bool AddToEventPool { get; set; } = true;

	[Property]
	[HideIf( "AddToEventPool", true )]
	public EventRarity Rarity { get; set; } = EventRarity.None;

	[Property]
	[Description( "Can this event trigger when there's another event going on? And can other events trigger whn this one is going on?" )]
	public bool Stackable { get; set; } = true;

	[Property]
	[Description( "If your event's end state is different from the start state (Objects moved or removed), reinstantiate everything when restarting. (Only this gameobject and all children on it)" )]
	public bool ReinstantiateOnRestart { get; set; } = false;

	/// <summary>
	/// Has the event been triggered
	/// </summary>
	public bool HasBeenPlayed { get; set; } = false;

	public int TimesPlayed = 0;

	/// <summary>
	/// Are any of the event components inside playing?
	/// </summary>
	public bool IsPlaying => Components.GetAll<EventComponent>( FindMode.EverythingInSelfAndChildren )
		.Any( x => x.IsPlaying );

	/// <summary>
	/// If all required event components have finished playing
	/// </summary>
	public bool IsFinished { get; set; } = false;


	bool _showToggle = false;
	JsonObject _initialState;

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
		if ( !GameManager.IsPlaying )
		{
			if ( Game.IsEditor )
			{
				foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
				{
					if ( component != this )
						component.Enabled = false;
				}

				_showToggle = false;
			}

			return;
		}

		foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
		{
			if ( component != this )
				component.Enabled = true; // Make sure to enable back all the components in case they were disabled
		}

		foreach ( var eventComponent in Components.GetAll<EventComponent>( FindMode.EverythingInSelfAndChildren ) )
		{
			foreach ( var trigger in eventComponent.Triggers )
				trigger.OnTrigger += HasBeenTriggered;
		}

		GameObject.BreakFromPrefab();

		if ( ReinstantiateOnRestart )
			_initialState = GameObject.Serialize();
	}

	protected override void OnFixedUpdate()
	{
		if ( !GameManager.IsPlaying ) return;

		var nowFinished = Components.GetAll<EventComponent>( FindMode.EverythingInSelfAndChildren )
			.All( x => x.Finished || (!x.RequiredToFinish && !x.IsPlaying) || !x.Enabled );

		if ( !IsFinished && nowFinished )
		{
			IsFinished = nowFinished;
			TimesPlayed++;

			End();
		}
	}

	public void End()
	{
		IsFinished = true;

		if ( ReinstantiateOnRestart )
		{
			foreach ( var child in GameObject.Children )
				child.Destroy();
		}
		else
		{
			foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( component != this )
					component.Enabled = false;
			}

			foreach ( var eventComponent in Components.GetAll<EventComponent>( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( !eventComponent.Triggered )
					eventComponent.Triggered = true;

				eventComponent.IsPlaying = false;
			}
		}
	}

	public void Restart()
	{
		IsFinished = false;

		if ( ReinstantiateOnRestart )
		{
			var substitute = new GameObject( true, GameObject.Name );
			var timesPlayed = TimesPlayed;
			var networked = GameObject.Networked;
			var parent = GameObject.Parent;
			var worldTransform = Transform.World;

			GameObject.DestroyImmediate();

			substitute.Deserialize( _initialState );
			substitute.Networked = networked;
			substitute.SetParent( parent );
			substitute.Transform.World = worldTransform;
			substitute.Components.Get<EventDefinition>().TimesPlayed = timesPlayed;
		}
		else
		{
			foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( component != this )
					component.Enabled = true;
			}

			foreach ( var eventComponent in Components.GetAll<EventComponent>( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( eventComponent.Triggered )
					eventComponent.Triggered = false;

				eventComponent.IsPlaying = false;
			}
		}
	}

	protected override void OnEnabled()
	{
		if ( !GameManager.IsPlaying ) return;
	}

	protected override void OnDisabled()
	{
		if ( !GameManager.IsPlaying ) return;

	}

	protected override void OnDestroy()
	{
	}

	void HasBeenTriggered( GameObject triggerer, GameObject targetObject = null )
	{
		HasBeenPlayed = true;
	}
}
