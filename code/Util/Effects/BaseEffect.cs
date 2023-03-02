namespace Sauna;

public class BaseEffect
{
	/// <summary>
	/// Amount of stacks currently on this effect.
	/// </summary>
	public int Stacks { get; set; }

	/// <summary>
	/// Duration of this effect in seconds.
	/// </summary>
	public float Duration { get; set; }

	/// <summary>
	/// The text displayed for this effect.
	/// </summary>
	public virtual string Text { get; }

	/// <summary>
	/// The path for this effect's icon.
	/// </summary>
	public virtual string IconPath { get; }

	/// <summary>
	/// The maximum duration for this effect in seconds.
	/// </summary>
	public virtual float MaxDuration { get; } = 15f;

	/// <summary>
	/// The maximum amount of stacks for this effect.
	/// </summary>
	public virtual int MaxStacks { get; } = 1;

	public virtual void Simulate( IClient cl ) { }

	public virtual void OnEnd( IClient cl ) { }
}
