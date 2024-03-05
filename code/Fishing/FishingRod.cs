namespace Sauna;

public sealed class FishingRod : Component
{
	[Property] public GameObject BobberPrefab;
	[Property] public float RetractDistance = 200;
	[Property] public float ThrowForce = 100;

	private GameObject CurrentBobber { get; set; }
	private GameObject Owner { get; set; }
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
		if (Casted && CurrentBobber.Transform.Position.Distance( Owner.Transform.Position ) > RetractDistance)
			RetractBobber();
	}

	private void OnInteract( Player player, GameObject obj )
	{
		if ( Casted )
		{
			RetractBobber();
		}
		else
		{
			Owner = player.GameObject;
			CurrentBobber = BobberPrefab.Clone();
			CurrentBobber.Enabled = true;
			CurrentBobber.Transform.Position = player.Transform.Position + player.Bounds.Center;
			CurrentBobber.Components.Get<Rigidbody>().Velocity = player.Velocity + player.EyeAngles.Forward * ThrowForce;
		}
	}

	private void RetractBobber()
	{
		CurrentBobber.Destroy();
		CurrentBobber = null;
		Owner = null;
	}
}
