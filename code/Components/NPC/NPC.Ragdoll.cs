using Sandbox;
using Sauna;

public partial class NPC
{
	public ModelPhysics Ragdoll => Model.Components.Get<ModelPhysics>();
	SkinnedModelRenderer _puppet;
	bool _isTransitioning = false;
	[Sync] RealTimeUntil _unragdoll { get; set; }
	Vector3 _lastPosition = Vector3.Zero;
	float _spin = 0f;

	/// <summary>
	/// Set the ragdoll state of the NPC
	/// </summary>
	/// <param name="ragdoll">Ragdoll or Unragdoll</param>
	/// <param name="duration">How long ragdoll state lasts</param>
	/// <param name="spin">How fast it spins towards the given velocity</param>
	[Broadcast( NetPermission.Anyone )]
	public void SetRagdoll( bool ragdoll, float duration = 2f, float spin = 0f )
	{
		InternalSetRagdoll( ragdoll, duration, spin );
	}

	async void InternalSetRagdoll( bool ragdoll, float duration = 2f, float spin = 0f )
	{
		await Task.Frame();

		if ( ragdoll )
		{
			if ( Ragdoll == null && Model != null && MoveHelper != null )
			{
				var newRagdoll = Model.Components.Create<ModelPhysics>();

				newRagdoll.Model = Model.Model;
				newRagdoll.Renderer = Model;

				newRagdoll.Enabled = false;
				newRagdoll.Enabled = true; // Gotta call OnEnabled for it to update :)

				newRagdoll.PhysicsGroup.Velocity = MoveHelper.Velocity;

				_puppet = Model.GameObject.Parent.Components.Create<SkinnedModelRenderer>();
				_puppet.Model = Model.Model;
				_puppet.Enabled = false;
				_puppet.Enabled = true;
				_puppet.SceneModel.RenderingEnabled = false;
				_puppet.Transform.Scale = Scale;

				foreach ( var collider in Components.GetAll<Collider>( FindMode.EverythingInSelfAndChildren ) )
					collider.Enabled = false;
			}

			_unragdoll = duration;
			_lastPosition = Ragdoll.Transform.World.Position;
			_spin = spin;
		}
		else
		{
			_unragdoll = 0f;
		}
	}

	void FollowRagdoll()
	{
		if ( Ragdoll == null ) return;

		var rootPosition = Transform.Position;

		if ( Model.GetAttachment( "foot_L" ) != null )
		{
			var leftFoot = Model.GetAttachment( "foot_L" ).Value.Position;
			var rightFoot = Model.GetAttachment( "foot_R" ).Value.Position;
			rootPosition = (leftFoot + rightFoot) / 2f;
		}

		if ( !_isTransitioning )
		{
			Transform.Position = rootPosition; // Remember to set before Renderer!
			Model.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset
		}

		var velocity = (Ragdoll.Transform.World.Position - _lastPosition);
		_lastPosition = Ragdoll.Transform.World.Position;

		if ( velocity.Length >= 10f && _unragdoll.Passed <= 0.2f )
		{
			var horizontalDirection = velocity.WithZ( 0f ).Normal;
			var rotatedDirection = horizontalDirection.RotateAround( 0f, Rotation.FromYaw( 90f ) );

			Ragdoll.PhysicsGroup.AngularVelocity = rotatedDirection * _spin;
		}

		if ( _unragdoll )
		{
			if ( !_isTransitioning )
			{
				_unragdoll = 0f;

				var groundTrace = Scene.Trace.Ray( rootPosition, rootPosition + Vector3.Down * 20f )
					.Size( 20f )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "player", "trigger", "npc" )
					.Run();

				if ( groundTrace.Hit )
					_isTransitioning = true;
			}
			else
			{
				var transition = 0.15f;

				var bones = _puppet.Model.Bones.AllBones;
				Dictionary<PhysicsBody, Transform> bodyTransforms = new();

				foreach ( var bone in bones )
				{
					var body = Ragdoll.PhysicsGroup.GetBody( bone.Index );

					if ( body != null )
						bodyTransforms.Add( body, body.Transform );
				}

				if ( _unragdoll.Passed <= transition )
				{
					if ( Components.TryGet<Character>( out var character, FindMode.EnabledInSelfAndChildren ) )
					{
						_puppet.SceneModel.Morphs.Set( "fat", character.Fatness );
						_puppet.Set( "height", character.Height );
					}

					var time = _unragdoll.Passed / transition;

					foreach ( var bone in bones )
					{
						if ( _puppet.TryGetBoneTransform( in bone, out var transform ) )
						{
							var body = Ragdoll.PhysicsGroup.GetBody( bone.Index );

							if ( body != null )
							{
								body.MotionEnabled = false;
								body.GravityEnabled = false;
								body.EnableSolidCollisions = false;
								body.LinearDrag = 9999f;
								body.AngularDrag = 9999f;

								var oldTransform = bodyTransforms[body];
								var newPos = oldTransform.Position.LerpTo( transform.Position, (float)time );
								var newRot = Rotation.Lerp( oldTransform.Rotation, transform.Rotation, (float)time );

								body.Position = newPos;
								body.Rotation = newRot;
							}
						}
					}
				}
				else
				{
					_puppet.Destroy();
					Ragdoll?.Destroy();

					Model.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset

					foreach ( var clothing in Model.GameObject.Children )
						clothing.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Clothing go offset too

					_isTransitioning = false;
				}
			}
		}
	}
}
