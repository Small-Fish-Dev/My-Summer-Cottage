using Sauna.Event;

namespace Sauna;

public sealed class Axe : Component
{
	[Property]
	public DamageType Type { get; set; } = DamageType.Average;

	[Property]
	public int Damage { get; set; } = 5;

	[Property]
	public SoundEvent SwingSound { get; set; }

	[Property]
	public SoundEvent WoodImpactSound { get; set; }

	protected override void OnStart()
	{
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction
		{
			Identifier = "item.used_1.Axe",
			Accessibility = AccessibleFrom.Hands,
			Cooldown = true,
			CooldownTime = 1f,
			ShowWhenDisabled = () => true,
			Action = OnSwing,
			DynamicText = () =>
			{
				if ( Player.Local.TargetedGameObject != null )
					if ( Player.Local.TargetedGameObject.Components.TryGet<ItemComponent>( out var item ) )
						if ( item.Name == "Wooden Log" )
							return "Chop log";
				return "Swing axe";
			},
			Keybind = "mouse1",
			Animation = InteractAnimations.Action,
			Sound = () => SwingSound,
		} ); ;
	}

	private async void OnSwing( Player player, GameObject obj )
	{
		await Task.Delay( 300 );

		if ( !player.IsValid() ) return;

		var target = player.TargetedGameObject ?? player.InteractionTrace.GameObject;
		if ( target is null )
			return;

		if ( target.Components.TryGet<ItemComponent>( out var item ) )
		{
			if ( item.Name == "Wooden Log" )
			{
				if ( PrefabLibrary.TryGetByPath( "prefabs/items/split_wooden_log.prefab", out var log ) )
				{
					GameObject.PlaySound( WoodImpactSound );
					var left = SceneUtility.GetPrefabScene( log.Prefab ).Clone( target.Transform.Position, target.Transform.Rotation );
					var right = SceneUtility.GetPrefabScene( log.Prefab ).Clone( target.Transform.Position, target.Transform.Rotation.RotateAroundAxis( Vector3.Forward, 180f ) );

					if ( left.Components.TryGet<Rigidbody>( out var leftBody ) )
						leftBody.Velocity = left.Transform.Rotation * Vector3.Right * 100f;
					if ( right.Components.TryGet<Rigidbody>( out var rightBody ) )
						rightBody.Velocity = right.Transform.Rotation * Vector3.Right * 100f;

					target.Destroy();

					TaskMaster.SubmitTriggerSignal( "item.used_1.Axe", player );
				}
			}
		}

		if ( target.Components.TryGet<Rigidbody>( out var body ) )
			body.ApplyImpulseAt( player.InteractionTrace.HitPosition, player.InteractionTrace.Direction * 300f );

		if ( target.Components.TryGet<HealthComponent>( out var health ) )
			health.Damage( Damage, Type, player.GameObject, player.InteractionTrace.HitPosition, player.InteractionTrace.Direction, 400f );
	}
}
