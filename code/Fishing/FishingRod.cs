using Sauna.SFX;

namespace Sauna;

public sealed class FishingRod : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public GameObject BobberPrefab;
	[Property] public float RetractDistance = 2500;
	[Property] public float ThrowForce = 1000;
	[Property] public SoundEvent CastSound { get; set; }

	public Player Owner { get; private set; }

	private Bobber CurrentBobber { get; set; }
	private LegacyParticles _fishingLine;

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
			InteractDistance = 160,
			DynamicText = () => IsCasted ? "Pull back" : "Cast",
			Keybind = "mouse1",
			Animation = InteractAnimations.Action,
			Sound = () => CastSound,
			Cooldown = true,
			CooldownTime = 1f,
			ShowWhenDisabled = () => true,
		} );
	}

	protected override void OnUpdate()
	{
		if ( Owner is not null )
		{
			if ( IsCasted && CurrentBobber.Transform.Position.Distance( Owner.Transform.Position ) > RetractDistance )
				RetractBobber( true );

			if ( !IsCasted )
				return;
		}

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

	private async void CastBobber( Player player )
	{
		await Task.Delay( 700 );
		IsCasted = true;
		Owner = player;
		GameObject newBobber;
		newBobber = BobberPrefab.Clone();
		newBobber.NetworkMode = NetworkMode.Object;
		newBobber.NetworkSpawn();
		CurrentBobber = newBobber.Components.Get<Bobber>();
		CurrentBobber.Rod = this;

		var transform = Renderer?.GetAttachment( "line" ) ?? global::Transform.Zero;
		CurrentBobber.Transform.Position = transform.Position + transform.Rotation.Forward * 10f;
		CurrentBobber.Components.Get<Rigidbody>().Velocity = player.Velocity + player.EyeAngles.Forward * ThrowForce;

		TaskMaster.SubmitTriggerSignal( "item.used_1.Fishing Rod", player );
	}

	private void RetractBobber( bool force = false )
	{
		if ( !force )
			CurrentBobber.PullOut();

		Cleanup();
	}

	protected override void OnDisabled()
	{
		Cleanup();
	}

	protected override void OnDestroy()
	{
		Cleanup();
	}

	private void Cleanup()
	{
		IsCasted = false;
		Owner = null;

		if ( CurrentBobber.IsValid() )
			CurrentBobber.GameObject.Destroy();
	}
}
