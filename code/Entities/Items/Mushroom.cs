namespace Sauna;

public partial class Mushroom : BaseItem
{
	public override string Model => "models/mushroom/mushroom.vmdl";
	public override string Title => "Silokkijak";
	public override Vector3 TitleOffset => Vector3.Up * 10f;

	public Mushroom()
	{
		var interactable = this as IInteractable;

		// TODO: Interactions.
	}

	public override void Spawn()
	{
		base.Spawn();

		PhysicsEnabled = false;
		Tags.Add( "nocollide" );
	}
}
