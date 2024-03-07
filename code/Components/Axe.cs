using Sauna.Event;

namespace Sauna;

public sealed class Axe : Component
{
	protected override void OnStart()
	{
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction
		{
			Identifier = "item.used_1.Axe",
			Accessibility = AccessibleFrom.Hands,
			Action = ( Player player, GameObject obj ) =>
			{
				var target = player.TargetedGameObject;
				if ( target != null )
					if ( target.Components.TryGet<ItemComponent>( out var item ) )
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
			Animation = InteractAnimations.Action
		} );
	}
}
