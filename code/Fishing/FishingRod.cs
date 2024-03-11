using Sauna.SFX;

namespace Sauna;

public sealed class FishingRod : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public GameObject BobberPrefab;
	[Property] public float RetractDistance = 750;
	[Property] public float ThrowForce = 1000;

	public Player Owner { get; private set; }

	private Bobber CurrentBobber { get; set; }
	private LegacyParticles _fishingLine;

	private readonly SoundEvent _castSound = ResourceLibrary.Get<SoundEvent>( "sounds/fishingrod/fishingrod_cast.sound" );

	private bool _isCasted;

	[Sync]
	public bool IsCasted
	{
		get => _isCasted;
		set
		{
			_isCasted = value;
			if ( _isCasted )
				_fishingLine = LegacyParticles.Create( "particles/basic_rope.vpcf", Transform.World );
			else
				_fishingLine?.Destroy();
		}
	}

	[Sync]
	private Vector3? BobberPosition { get; set; }

	protected override void OnStart()
	{
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction
		{
			Accessibility = AccessibleFrom.Hands,
			Action = OnInteract,
			DynamicText = () => IsCasted ? "Pull back" : "Cast",
			Keybind = "mouse1",
			Animation = InteractAnimations.Action,
			Sound = () => _castSound,
			Cooldown = true,
			CooldownTime = 1f,
			ShowWhenDisabled = () => true,
		} );
	}

	protected override void OnUpdate()
	{
		if ( Owner is null )
			return;

		if ( IsCasted && CurrentBobber.Transform.Position.Distance( Owner.Transform.Position ) > RetractDistance )
			RetractBobber( true );

		if ( !IsCasted )
			return;

		if ( CurrentBobber.IsValid() )
			BobberPosition = CurrentBobber.Transform.Position;

		if ( BobberPosition is not null && _fishingLine is not null && _fishingLine.GameObject.IsValid() )
		{
			_fishingLine.Transform = Renderer?.GetAttachment( "line" ) ?? global::Transform.Zero;
			_fishingLine.SetVector( 1, BobberPosition.Value );
		}
	}

	private void OnInteract( Player player, GameObject obj )
	{
		if ( IsCasted )
			RetractBobber();
		else
			CastBobber( player );
	}

	private void CastBobber( Player player )
	{
		IsCasted = true;
		Owner = player;
		GameObject newBobber;
		newBobber = BobberPrefab.Clone();
		newBobber.NetworkSpawn();
		CurrentBobber = newBobber.Components.Get<Bobber>();
		CurrentBobber.Rod = this;

		var transform = Renderer?.GetAttachment( "line" ) ?? global::Transform.Zero;
		CurrentBobber.Transform.Position = transform.Position + transform.Rotation.Forward * 10f;
		CurrentBobber.Components.Get<Rigidbody>().Velocity = player.Velocity + player.EyeAngles.Forward * ThrowForce;
	}

	private void RetractBobber( bool force = false )
	{
		if ( !force )
			CurrentBobber.PullOut();

		CurrentBobber.GameObject.Destroy();
		Owner = null;
		IsCasted = false;
	}

	protected override void OnDestroy()
	{
		IsCasted = false;

		if ( CurrentBobber.IsValid() )
			CurrentBobber.GameObject.Destroy();
	}
}
