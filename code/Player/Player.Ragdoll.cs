﻿using Sandbox;
using static Sandbox.PhysicsGroupDescription.BodyPart;

namespace Sauna;

partial class Player
{

	/// <summary>
	/// Is the player in ragdoll mode
	/// </summary>
	public bool IsRagdolled => Ragdoll.IsValid();

	/// <summary>
	/// Can the player ragdoll or unragdoll themselves or not
	/// </summary>
	public bool CanRagdoll { get; set; } = true;

	public bool RagdollDisable { get; set; } = false;

	public ModelPhysics Ragdoll => Renderer.Components.Get<ModelPhysics>();
	SkinnedModelRenderer _puppet;
	bool _isTransitioning = false;
	float _oldAirFriction = 1f;
	[Sync] RealTimeUntil _unragdoll { get; set; }
	bool _couldRagdoll = true;
	Vector3 _lastPosition = Vector3.Zero;
	float _spin = 10f;

	/// <summary>
	/// Set the ragdoll state of the player
	/// </summary>
	/// <param name="ragdoll">Ragdoll or Unragdoll</param>
	/// <param name="forced">Force the player to go through ragdoll state</param>
	/// <param name="duration">How long ragdoll state lasts</param>
	/// <param name="spin">How fast it spins towards the given velocity</param>
	[Broadcast( NetPermission.Anyone )]
	public void SetRagdoll( bool ragdoll, bool forced = true, float duration = 2f, float spin = 10f )
	{
		if ( RagdollDisable ) return;

		if ( ragdoll )
		{
			if ( Ragdoll == null )
			{
				var newRagdoll = Renderer.Components.Create<ModelPhysics>();

				newRagdoll.Model = Renderer.Model;
				newRagdoll.Renderer = Renderer;

				newRagdoll.Enabled = false;
				newRagdoll.Enabled = true; // Gotta call OnEnabled for it to update :)

				newRagdoll.PhysicsGroup.Velocity = MoveHelper.Velocity;

				_puppet = Renderer.GameObject.Parent.Components.Create<SkinnedModelRenderer>();
				_puppet.Model = Renderer.Model;
				_puppet.Enabled = false;
				_puppet.Enabled = true;
				_puppet.SceneModel.RenderingEnabled = false;

				_oldAirFriction = MoveHelper.AirFriction;
				MoveHelper.AirFriction = 0f;

				var collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndAncestors );
				collider.Enabled = false;
			}

			BlockMovements = true;

			_unragdoll = duration;
			_couldRagdoll = CanRagdoll;
			CanRagdoll = !forced;
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
		if ( !IsRagdolled ) return;

		Ducking = true;

		var leftFoot = Renderer.GetAttachment( "foot_L" ).Value.Position;
		var rightFoot = Renderer.GetAttachment( "foot_R" ).Value.Position;
		var rootPosition = (leftFoot + rightFoot) / 2f;

		if ( !_isTransitioning )
		{
			Transform.Position = rootPosition; // Remember to set before Renderer!
			Renderer.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset
		}

		var velocity = (Ragdoll.Transform.World.Position - _lastPosition);
		_lastPosition = Ragdoll.Transform.World.Position;

		if ( velocity.Length >= 10f )
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

				var groundTrace = Scene.Trace.Ray( rootPosition + Vector3.Up * 30f, rootPosition + Vector3.Down * 40f )
					.Size( 20f )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "player", "trigger", "npc" )
					.Run();

				if ( groundTrace.Hit )
					_isTransitioning = true;

				foreach ( var body in Ragdoll.PhysicsGroup.Bodies )
				{
					body.GravityEnabled = false;
					body.MotionEnabled = false;
				}
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
					_puppet.Set( "grounded", MoveHelper.IsOnGround );
					_puppet.Set( "crouching", Ducking );
					_puppet.SceneModel.Morphs.Set( "fat", Fatness );
					_puppet.Set( "height", Height );

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

					Renderer.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset

					foreach ( var clothing in Renderer.GameObject.Children )
						clothing.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Clothing go offset too

					MoveHelper.AirFriction = _oldAirFriction;

					var collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndAncestors );
					collider.Enabled = true;

					_isTransitioning = false;
					BlockMovements = false;
					CanRagdoll = _couldRagdoll;
				}
			}
		}
	}
}
