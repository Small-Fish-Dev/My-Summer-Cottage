namespace Sauna.Fishing;

public sealed class FishingRod : Component
{
	[Property] public GameObject BobberPrefab;
	[Property] public float RetractDistance = 200;
	[Property] public float ThrowForce = 100;

	public Player Owner { get; private set; }

	private Bobber CurrentBobber { get; set; }
	public bool Casted => CurrentBobber.IsValid();

	protected override void OnStart()
	{
		var interactions = Components.GetOrCreate<Interactions>();

		interactions.AddInteraction( new Interaction
		{
			Accessibility = AccessibleFrom.Hands,
			Action = OnInteract,
			DynamicText = () => Casted ? "Pull back" : "Cast",
			Keybind = "mouse1",
			Animation = InteractAnimations.Action
		} );
	}

	protected override void OnUpdate()
	{
		if ( Casted && CurrentBobber.Transform.Position.Distance( Owner.Transform.Position ) > RetractDistance )
			RetractBobber( true );
	}

	private void OnInteract( Player player, GameObject obj )
	{
		if ( Casted )
		{
			RetractBobber();
		}
		else
		{
			Owner = player;
			var newBobber = BobberPrefab.Clone();
			newBobber.Enabled = true;
			newBobber.Transform.Position = player.Transform.Position + player.Bounds.Center;
			newBobber.Components.Get<Rigidbody>().Velocity = player.Velocity + player.EyeAngles.Forward * ThrowForce;
			CurrentBobber = newBobber.Components.Get<Bobber>();
			CurrentBobber.Rod = this;
		}
	}

	private void RetractBobber( bool force = false )
	{
		if ( !force )
			CurrentBobber.PullOut();

		CurrentBobber.GameObject.Destroy();
		CurrentBobber = null;
		Owner = null;
	}
}
