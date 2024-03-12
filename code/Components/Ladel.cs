using Sauna.Event;

namespace Sauna;

public sealed class Ladel : Component
{
	[Sync]
	public bool HasWater { get; set; } = false;

	protected override void OnStart()
	{
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction
		{
			Identifier = "",
			Accessibility = AccessibleFrom.Hands,
			Cooldown = true,
			CooldownTime = 1f,
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

	void LadelInteract( Player player, GameObject obj )
	{
		if ( HasWater )
		{
			if ( player.TargetedGameObject != null )
				if ( player.TargetedGameObject.Components.TryGet<Stove>( out var stove ) )
					if ( !stove.HasWater && stove.HasWood )
					{
						HasWater = false;
						stove.HasWater = true;
					}

			HasWater = false;
		}
		else
		{
			if ( player.TargetedGameObject != null )
				if ( player.TargetedGameObject.Components.TryGet<WaterBucket>( out var bucket ) )
					HasWater = true;
		}
	}
}
