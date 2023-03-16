namespace Sauna;

public class Drunkness : BaseEffect
{
	public override string Text => "Drunkness";
	public override float MaxDuration => 30f;
	public override int MaxStacks => 5;

	public override void Simulate( Player pawn )
	{
		base.Simulate( pawn );
	}
}
