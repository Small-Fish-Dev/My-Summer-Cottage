namespace Sauna;

public partial class Player
{
	private const float INTERACTION_DISTANCE = 100f;
	private const float INTERACTION_SIZE = 10f;

	public Ray ViewRay => new( Camera.Transform.Position, Camera.Transform.Rotation.Forward );
	public GameObject TargetedGameObject { get; private set; }
	public SceneTraceResult InteractionTrace { get; private set; }
	public BBox? InteractionBounds { get; private set; }

	private void UpdateInteractions()
	{
		var thinTrace = Scene.Trace.Ray( ViewRay, INTERACTION_DISTANCE )
					.IgnoreGameObject( GameObject )
					.WithoutTags( "world" )
					.Run();

		if ( thinTrace.GameObject != null && thinTrace.GameObject.GetInteractions() != null )
		{
			InteractionTrace = thinTrace;
			InteractionBounds = thinTrace.GameObject != TargetedGameObject ? null : InteractionBounds;
			TargetedGameObject = thinTrace.GameObject;

			if ( InteractionBounds == null && TargetedGameObject.IsValid() )
			{
				if ( TargetedGameObject.Components.TryGet<BoxCollider>( out var box ) )
					InteractionBounds = new BBox( box.Center - box.Scale / 2f, box.Center + box.Scale / 2f );

				if ( TargetedGameObject.Components.TryGet<CapsuleCollider>( out var capsule ) )
					InteractionBounds = new BBox( capsule.Start - capsule.Radius, capsule.End + capsule.Radius );

				if ( TargetedGameObject.Components.TryGet<ModelCollider>( out var model ) )
					InteractionBounds = model.Model.PhysicsBounds;
			}
		}
		else
		{
			InteractionTrace = Scene.Trace.Ray( ViewRay, INTERACTION_DISTANCE )
						.Size( INTERACTION_SIZE )
						.IgnoreGameObject( GameObject )
						.WithoutTags( "world" )
						.Run();
			var obj = InteractionTrace.GameObject;
			obj = obj?.GetInteractions() == null ? null : obj;

			InteractionBounds = InteractionTrace.GameObject != TargetedGameObject ? null : InteractionBounds;
			TargetedGameObject = obj;

			if ( InteractionBounds == null && TargetedGameObject.IsValid() )
			{
				if ( TargetedGameObject.Components.TryGet<BoxCollider>( out var box ) )
					InteractionBounds = new BBox( box.Center - box.Scale / 2f, box.Center + box.Scale / 2f );

				if ( TargetedGameObject.Components.TryGet<CapsuleCollider>( out var capsule ) )
					InteractionBounds = new BBox( capsule.Start - capsule.Radius, capsule.End + capsule.Radius );

				if ( TargetedGameObject.Components.TryGet<ModelCollider>( out var model ) )
					InteractionBounds = model.Model.PhysicsBounds;
			}
		}

		// Get bounds again.
		if ( InteractionBounds == null && TargetedGameObject != null )
		{
			InteractionBounds = BBox.FromPositionAndSize( 0, 50f );
		}
	}

	// A lot of parameters! We should fix this up at a later point.
	[Broadcast]
	public void BroadcastInteraction(
		Vector3 position,
		Rotation rotation,
		InteractAnimations animation,
		Guid interactionObjectId,
		int soundResourceId,
		bool playSoundFromPlayer
	)
	{
		if ( animation == InteractAnimations.Interact )
		{
			Renderer.Set( "right_ik_pos", position );
			Renderer.Set( "right_ik_rot", rotation );
			Renderer.Set( "use", true );
		}
		else if ( animation == InteractAnimations.Action )
		{
			ForceHoldType( HoldType, 0.5f );
			Renderer.Set( "right_ik_pos", position );
			Renderer.Set( "right_ik_rot", rotation );
			Renderer.Set( "action", true );
		}
		else if ( animation == InteractAnimations.Shoot )
		{
			Renderer.Set( "shoot", true );
		}
		else if ( animation == InteractAnimations.Reload )
		{
			Renderer.Set( "reload", true );
		}

		var soundEvent = ResourceLibrary.Get<SoundEvent>( soundResourceId );
		if ( soundEvent is null )
			return;

		var interactionGameObject = Game.ActiveScene.GetAllObjects( true )
			.Where( x => x.Id == interactionObjectId )
			.FirstOrDefault();

		if ( interactionGameObject is null || playSoundFromPlayer )
			GameObject.PlaySound( soundEvent );
		else
			interactionGameObject.PlaySound( soundEvent );
	}
}
