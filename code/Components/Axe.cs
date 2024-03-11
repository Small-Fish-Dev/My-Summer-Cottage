using Sauna.Event;

namespace Sauna;

public sealed class Axe : Component
{
	[Property]
	public DamageType Type { get; set; } = DamageType.Average;

	[Property]
	public int Damage { get; set; } = 5;

	private readonly SoundEvent _swingSound = ResourceLibrary.Get<SoundEvent>( "sounds/axe/axe_swing.sound" );

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
			Action = ( Player player, GameObject obj ) =>
			{
				var target = player.TargetedGameObject ?? player.InteractionTrace.GameObject;

				if ( target != null )
				{
					if ( target.Components.TryGet<ItemComponent>( out var item ) )
					{
						if ( item.Name == "Wooden Log" )
						{
							if ( PrefabLibrary.TryGetByPath( "prefabs/items/split_wooden_log.prefab", out var log ) )
							{
								SceneUtility.GetPrefabScene( log.Prefab ).Clone( target.Transform.Position, target.Transform.Rotation );
								SceneUtility.GetPrefabScene( log.Prefab ).Clone( target.Transform.Position, target.Transform.Rotation.RotateAroundAxis( Vector3.Forward, 180f ) );
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
			},
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
			Sound = () => _swingSound,
		} ); ;
	}
}
