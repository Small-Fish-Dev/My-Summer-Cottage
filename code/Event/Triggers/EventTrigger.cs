using Microsoft.VisualBasic;
using Sandbox;
using static EventComponent;
using static Sauna.Event.EventTrigger;

namespace Sauna.Event;

[Hide]
public abstract class EventTrigger : Component
{
	/// <summary>
	/// Give this trigger a name to shoot a signal that gets picked up by the task master when triggered
	/// </summary>
	[Property]
	public string TriggerSignalIdentifier { get; set; }
	public event SaunaEvent OnTrigger;

	public virtual bool IsPolled { get; set; } = false;

	/// <summary>
	/// Maximum ticks it can go without running the polled method
	/// </summary>
	[Property, HideIf( "IsPolled", false )]
	public virtual int MaxPollingRate { get; set; } = 5;

	public int LastPoll = 0;

	/// <summary>
	/// How much time passed since the last poll
	/// </summary>
	public float Delta => Time.Delta + LastPoll * Time.Delta;

	protected override void OnStart()
	{
	}

	protected override void OnUpdate()
	{
	}

	/// <summary>
	/// Run polled code here
	/// </summary>
	public virtual void PolledMethod()
	{

	}

	/// <summary>
	/// Resets the considtions needed to trigger this event
	/// </summary>
	public virtual void Clear()
	{

	}

	// Can't invoke events outside of the class itself even if it derives from this, so we have this method instead
	public virtual void CallTrigger( GameObject triggerer, GameObject targetObject = null )
	{
		OnTrigger?.Invoke( triggerer, targetObject );

		if ( TriggerSignalIdentifier != null )
		{
			var playerComponent = triggerer.Components.Get<Player>();

			if ( playerComponent != null )
				TaskMaster.SubmitTriggerSignal( TriggerSignalIdentifier, playerComponent );
		}
	}
}
