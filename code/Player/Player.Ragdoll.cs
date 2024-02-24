using static Sandbox.PhysicsGroupDescription.BodyPart;

namespace Sauna;

partial class Player
{

	/// <summary>
	/// Is the player in ragdoll mode
	/// </summary>
	public bool IsRagdolled => Ragdoll.IsValid();

	public ModelPhysics Ragdoll => Renderer.Components.Get<ModelPhysics>();
	SkinnedModelRenderer _puppet;
	bool _isTransitioning = false;
	float _oldAirFriction = 1f;

	[Broadcast]
	public void SetRagdoll( bool ragdoll, bool blockInputs = true )
	{
		if ( ragdoll )
		{
			Ragdoll?.Destroy(); // Can never be too safe, what if there's one already??

			var newRagdoll = Renderer.Components.Create<ModelPhysics>();

			newRagdoll.Model = Renderer.Model;
			newRagdoll.Renderer = Renderer;

			newRagdoll.Enabled = false;
			newRagdoll.Enabled = true; // Gotta call OnEnabled for it to update :)

			newRagdoll.PhysicsGroup.Velocity = MoveHelper.Velocity;

			_oldAirFriction = MoveHelper.AirFriction;
			MoveHelper.AirFriction = 0f;

			var collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndAncestors );
			collider.Enabled = false;
		}
		else
		{
			DeleteRagdoll();
		}

		BlockMovements = ragdoll;
	}

	async void DeleteRagdoll()
	{
		_puppet = Renderer.GameObject.Parent.Components.Create<SkinnedModelRenderer>();
		_puppet.Model = Renderer.Model;
		_puppet.Enabled = false;
		_puppet.Enabled = true;
		_puppet.SceneModel.RenderingEnabled = false;

		_isTransitioning = true;

		TimeSince timeSince = 0f;

		var transition = 0.15f;


		var bones = _puppet.Model.Bones.AllBones;
		Dictionary<PhysicsBody, Transform> bodyTransforms = new();

		foreach ( var bone in bones )
		{
			var body = Ragdoll.PhysicsGroup.GetBody( bone.Index );

			if ( body != null )
				bodyTransforms.Add( body, body.Transform );
		}

		while ( timeSince <= transition )
		{
			_puppet.Set( "grounded", MoveHelper.IsOnGround );
			_puppet.Set( "crouching", Ducking );
			_puppet.SceneModel.Morphs.Set( "fat", Fatness );
			_puppet.Set( "height", Height );

			var time = timeSince / transition;

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
						var newPos = oldTransform.Position.LerpTo( transform.Position, time );
						var newRot = Rotation.Lerp( oldTransform.Rotation, transform.Rotation, time );

						body.Position = newPos;
						body.Rotation = newRot;
					}
				}
			}

			await Task.Frame();
		}

		_puppet.Destroy();
		Ragdoll?.Destroy();

		Renderer.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset

		foreach ( var clothing in Renderer.GameObject.Children )
			clothing.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Clothing go offset too

		MoveHelper.AirFriction = _oldAirFriction;

		var collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndAncestors );
		collider.Enabled = true;

		_isTransitioning = false;
	}

	void FollowRagdoll()
	{
		if ( !IsRagdolled ) return;

		var leftFoot = Renderer.GetAttachment( "foot_L" ).Value.Position;
		var rightFoot = Renderer.GetAttachment( "foot_R" ).Value.Position;
		var rootPosition = (leftFoot + rightFoot) / 2f;

		if ( !_isTransitioning )
		{
			Transform.Position = rootPosition; // Remember to set before Renderer!
			Renderer.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset
		}

		Ducking = true;
	}
}
