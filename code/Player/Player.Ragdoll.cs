namespace Sauna;

partial class Player
{

	/// <summary>
	/// Is the player in ragdoll mode
	/// </summary>
	[Sync]
	public bool IsRagdolled { get; private set; } = false;

	public ModelPhysics Ragdoll => Renderer.Components.Get<ModelPhysics>();

	float oldAirFriction;

	[Broadcast]
	public void SetRagdoll( bool ragdoll )
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

			oldAirFriction = MoveHelper.AirFriction;

			MoveHelper.AirFriction = 0f;
		}
		else
		{
			Ragdoll?.Destroy();

			Renderer.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset

			foreach ( var clothing in Renderer.GameObject.Children )
				clothing.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Clothing go offset too

			MoveHelper.AirFriction = oldAirFriction;
		}

		BlockMovements = ragdoll;
		IsRagdolled = ragdoll;
	}

	void FollowRagdoll()
	{
		if ( !IsRagdolled ) return;

		Renderer.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset
		Transform.Position = Ragdoll.Transform.Position;
		Ducking = true;
	}
}
