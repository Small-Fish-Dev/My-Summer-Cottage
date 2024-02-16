namespace Sauna;

public class Unconscious : BaseEffect
{
	public override string Text => "Unconscious";

	public Vector3 Force { get; set; }
	public bool SelfApplied { get; set; }

	public override void Simulate()
	{

	}

	public override void OnEnd()
	{

	}
}
