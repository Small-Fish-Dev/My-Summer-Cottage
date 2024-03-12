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
			DynamicText = () =>
			{
				var toReturn = "Swing";

				if ( HasWater )
				{
					if ( Player.Local.TargetedGameObject != null )
						if ( Player.Local.TargetedGameObject.Components.TryGet<Stove>( out var stove ) )
							if ( !stove.HasWater && stove.HasWood )
								toReturn = "Pour water";

					toReturn = "Throw water";
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

		if ( HasWater )
		{
			if ( target != null )
				if ( target.Components.TryGet<Stove>( out var stove ) )
					if ( !stove.HasWater && stove.HasWood )
					{
						HasWater = false;
						stove.HasWater = true;
					}

			HasWater = false;
		}
		else
		{
			if ( target != null )
				if ( target.Components.TryGet<WaterBucket>( out var bucket ) )
					HasWater = true;
		}

		if ( target.Components.TryGet<HealthComponent>( out var health ) )
			health.Damage( 1, DamageType.Mild, player.GameObject, player.InteractionTrace.HitPosition, player.InteractionTrace.Direction, 200f );

		if ( target.Components.TryGet<Rigidbody>( out var body ) )
			body.ApplyImpulseAt( player.InteractionTrace.HitPosition, player.InteractionTrace.Direction * 100f );
	}

	protected override void OnFixedUpdate()
	{
		_model?.SetBodyGroup( "water", HasWater ? 1 : 0 );
	}
}
