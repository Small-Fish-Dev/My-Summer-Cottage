using Sandbox;
using Sauna.Event;

[Icon( "flash_on" )]
[Category( "Events" )]
public sealed class EventComponent : Component
{
	[Property]
	public List<EventTrigger> Triggers { get; set; }

	[Property]
	public Action<GameObject> Event { get; set; }

	/// <summary>
	/// This event component is required to play before the event can be considered finished
	/// </summary>
	[Property]
	public bool RequiredToFinish { get; set; } = true;

	/// <summary>
	/// When this event component triggers, it disables all of these other ones that are now unnecessary
	/// </summary>
	[Property]
	public List<EventComponent> DisableOnTrigger { get; set; } = new();

	bool _triggered = false;

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
				{
					trigger.OnTrigger -= Event;
					trigger.OnTrigger -= HasBeenTriggered;
				}
			}

			if ( value == false && Triggered )
			{
				foreach ( var trigger in Triggers )
				{
					trigger.OnTrigger += Event;
					trigger.OnTrigger += HasBeenTriggered;
				}
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

	void HasBeenTriggered( GameObject _ )
	{
		Triggered = true;
		IsPlaying = true;

		foreach ( var eventComponent in DisableOnTrigger )
		{
			eventComponent.Triggered = true;
			eventComponent.Enabled = false;
		}
	}

	protected override void OnEnabled()
	{
		if ( Triggered )
			Triggered = false; // Reset if it's reenabled
		else
		{
			foreach ( var trigger in Triggers )
			{
				trigger.OnTrigger += Event;
				trigger.OnTrigger += HasBeenTriggered;
			}
		}
	}

	protected override void OnDisabled()
	{
		IsPlaying = false;

		foreach ( var trigger in Triggers )
		{
			trigger.OnTrigger -= Event;
			trigger.OnTrigger -= HasBeenTriggered;
		}
	}

	protected override void OnUpdate()
	{
	}
}
