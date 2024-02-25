using Sandbox;
using static Sauna.Event.EventTrigger;

namespace Sauna.Event;

[Hide]
public abstract class EventTrigger : Component
{
	public event Action<GameObject> OnTrigger;

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
	protected virtual void CallTrigger( GameObject triggerer )
	{
		OnTrigger?.Invoke( triggerer );
	}
}
