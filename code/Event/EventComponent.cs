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

	bool _triggered = false;

	/// <summary>
	/// Has this event component been triggered?
	/// </summary>
	public bool Triggered
	{
		get => _triggered;
		set
		{
			if ( value == true )
			{
				foreach ( var trigger in Triggers )
				{
					trigger.OnTrigger -= Event;
					trigger.OnTrigger -= HasBeenTriggered;
				}
			}

			if ( value == false )
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


	protected override void OnStart()
	{
		foreach ( var trigger in Triggers )
		{
			trigger.OnTrigger += Event;
			trigger.OnTrigger += HasBeenTriggered;
		}
	}

	void HasBeenTriggered( GameObject _ )
	{
		Triggered = true;
		IsPlaying = true;
	}

	protected override void OnEnabled()
	{
		if ( Triggered )
			Triggered = false; // Reset if it's reenabled
	}

	protected override void OnDisabled()
	{
		IsPlaying = false;
	}

	protected override void OnUpdate()
	{
	}
}
