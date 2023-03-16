namespace Sauna;

public class Unconscious : BaseEffect
{
	public override string Text => "Unconscious";

	public Vector3 Force { get; set; }
	public bool SelfApplied { get; set; }

	public override void Simulate( Player pawn )
	{
		if ( pawn.Ragdoll == null || !pawn.Ragdoll.IsValid )
			pawn.SetRagdoll( true, Force );
	}

	public override void OnEnd( Player pawn )
	{
		pawn.SetRagdoll( false );
	}
}
