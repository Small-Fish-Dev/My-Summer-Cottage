using Sauna.Event;
using static Sandbox.PhysicsContact;

namespace Sauna;

public sealed class Ladel : Component
{
	[Sync]
	public bool HasWater { get; set; } = false;

	ModelRenderer _model { get; set; }

	protected override void OnStart()
	{
		_model = Components.Get<ModelRenderer>();

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction
		{
			Identifier = "",
			Accessibility = AccessibleFrom.Hands,
			Cooldown = true,
			CooldownTime = 0.5f,
			ShowWhenDisabled = () => true,
			Action = LadelInteract,
			InteractDistance = 80,
			DynamicText = () =>
			{
				var toReturn = "Swing";

				if ( HasWater )
				{
					toReturn = "Throw water";

					if ( Player.Local.TargetedGameObject != null )
						if ( Player.Local.TargetedGameObject.Components.TryGet<Stove>( out var stove ) )
							if ( !stove.HasWater && stove.HasWood )
								toReturn = "Pour water";
				}
				else
				{
					if ( Player.Local.TargetedGameObject != null )
						if ( Player.Local.TargetedGameObject.Components.TryGet<WaterBucket>( out var bucket ) )
							toReturn = "Collect water";
				}

				return toReturn;
			},
			Keybind = "mouse1",
			Animation = InteractAnimations.Action
		} ); ;
	}

	async void LadelInteract( Player player, GameObject obj )
	{
		await Task.Delay( 300 );

		var target = player.TargetedGameObject;

		if ( target != null )
		{
			if ( HasWater )
			{
				if ( target != null )
					if ( target.Components.TryGet<Stove>( out var stove ) )
						if ( !stove.HasWater && stove.HasWood )
						{
							HasWater = false;
							stove.HasWater = true;
							TaskMaster.SubmitTriggerSignal( "item.used_2.Ladel", player );
						}

				HasWater = false;
			}
			else
			{
				if ( target != null )
					if ( target.Components.TryGet<WaterBucket>( out var bucket ) )
					{
						TaskMaster.SubmitTriggerSignal( "item.used_1.Ladel", player );
						HasWater = true;
					}
			}

			if ( target.Components.TryGet<HealthComponent>( out var health ) )
				health.Damage( 1, DamageType.Mild, player.GameObject, player.InteractionTrace.HitPosition, player.InteractionTrace.Direction, 200f );

			if ( target.Components.TryGet<Rigidbody>( out var body ) )
				body.ApplyImpulseAt( player.InteractionTrace.HitPosition, player.InteractionTrace.Direction * 100f );
		}
	}

	protected override void OnFixedUpdate()
	{
		_model?.SetBodyGroup( "water", HasWater ? 1 : 0 );
	}
}
