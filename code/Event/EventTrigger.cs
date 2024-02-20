using Sandbox;

[Hide]
public abstract class EventTrigger : Component
{
	public virtual event Action<GameObject> OnTrigger;

	protected override void OnStart()
	{
	}

	protected override void OnUpdate()
	{

	}

	// Can't invoke events outside of the class itself even if it derives from this, so we have this method instead
	protected void CallTrigger( GameObject triggerer )
	{
		OnTrigger?.Invoke( triggerer );
	}
}
