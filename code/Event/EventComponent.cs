using Sandbox;
using Sauna;
using Sauna.Event;

[Icon( "flash_on" )]
[Category( "Events" )]
public sealed class EventComponent : Component
{
	[Property]
	public List<EventTrigger> Triggers { get; set; }

	[Property]
	public SaunaEvent Event { get; set; }
	public delegate void SaunaEvent( GameObject triggerer, GameObject targetObject );

	/// <summary>
	/// This event component is required to play before the event can be considered finished
	/// </summary>
	[Property]
	public bool RequiredToFinish { get; set; } = true;

	/// <summary>
	/// When this event component is playing, the ones selected will be disabled, then reenabled when done
	/// </summary>
	[Property]
	public List<EventComponent> DisabledWhilePlaying { get; set; } = new();

	/// <summary>
	/// When this event component triggers, it disables all of these other ones that are now unnecessary
	/// </summary>
	[Property]
	public List<EventComponent> DisableOnTrigger { get; set; } = new();

	/// <summary>
	/// Once triggered, it won't trigger again until reset. False will let this event trigger as many times 
	/// </summary>
	[Property]
	public bool TriggerOnce { get; set; } = true;

	/// <summary>
	/// If it can get triggered multiple times, set a cooldown in between triggers.
	/// </summary>
	[Property]
	[Range( 0f, 10f, 0.1f )]
	[ShowIf( "TriggerOnce", false )]
	public float TriggerCooldown { get; set; } = 1f;

	bool _triggered = false;
	TimeSince _lastTrigger = 0f;

	/// <summary>
	/// Has this event component been triggered?
	/// </summary>
	public bool Triggered
	{
		get => _triggered;
		set
		{
			if ( value == true && !Triggered )
			{
				foreach ( var trigger in Triggers )
					trigger.OnTrigger -= HasBeenTriggered;
			}

			if ( value == false && Triggered )
			{
				foreach ( var trigger in Triggers )
					trigger.OnTrigger += HasBeenTriggered;
			}

			_triggered = value;
		}
	}

	/// <summary>
	/// Is this event component currently playing? Needs an event end node in the actiongraph to end
	/// </summary>
	public bool IsPlaying { get; set; } = false;

	/// <summary>
	/// Has this event component both been triggered and done playing
	/// </summary>
	public bool Finished => Triggered && !IsPlaying;

	List<EventComponent> _eventsDisabled = new();

	void HasBeenTriggered( GameObject triggerer, GameObject targetObject = null )
	{
		if ( TriggerOnce || (!TriggerOnce && _lastTrigger >= TriggerCooldown) )
		{
			Triggered = true;
			IsPlaying = true;

			_lastTrigger = 0f;

			foreach ( var eventComponent in DisableOnTrigger )
			{
				eventComponent.Triggered = true;
				eventComponent.Enabled = false;
			}

			foreach ( var eventComponent in DisabledWhilePlaying )
			{
				if ( eventComponent.Enabled )
				{
					eventComponent.Enabled = false;
					_eventsDisabled.Add( eventComponent );
				}
			}

			if ( !TriggerOnce )
			{
				foreach ( var trigger in Triggers )
					trigger.Clear();
			}

			Event?.Invoke( triggerer, targetObject );
		}
	}

	public async void Finish()
	{
		await Task.Frame();

		IsPlaying = false;

		foreach ( var eventToEnable in _eventsDisabled )
		{
			eventToEnable.Enabled = true;
		}

		_eventsDisabled.Clear();

		if ( !TriggerOnce )
			Triggered = false;
	}

	protected override void OnEnabled()
	{
		if ( Triggered )
			Triggered = false; // Reset if it's reenabled
		else
		{
			foreach ( var trigger in Triggers )
			{
				trigger.OnTrigger += HasBeenTriggered;
				trigger.Clear();
			}
		}
	}

	protected override void OnDisabled()
	{
		IsPlaying = false;

		if ( TriggerOnce )
		{
			foreach ( var trigger in Triggers )
			{
				trigger.OnTrigger -= HasBeenTriggered;
				trigger.Clear();
			}
		}
	}

	protected override void OnUpdate()
	{
	}
}
