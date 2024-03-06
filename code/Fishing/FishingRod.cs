using Sauna.SFX;

namespace Sauna.Fishing;

public sealed class FishingRod : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public GameObject BobberPrefab;
	[Property] public float RetractDistance = 200;
	[Property] public float ThrowForce = 100;
	
	public Player Owner { get; private set; }

	private Bobber CurrentBobber { get; set; }
	private LegacyParticles _fishingLine;
	
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

	protected override void OnPreRender()
	{
		if ( CurrentBobber.IsValid() && _fishingLine != null )
		{
			_fishingLine.Transform = Renderer?.GetAttachment( "line" ) ?? global::Transform.Zero;
			_fishingLine.SetVector( 1, CurrentBobber.Transform.Position );
		}
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

			_fishingLine = LegacyParticles.Create( "particles/basic_rope.vpcf", Transform.World );
		}
	}

	private void RetractBobber( bool force = false )
	{
		if ( !force )
			CurrentBobber.PullOut();

		CurrentBobber.GameObject.Destroy();
		CurrentBobber = null;
		Owner = null;
		_fishingLine?.Destroy();
	}
}
