using Sandbox;
using Sauna;
using static NPC;

public enum DamageType
{
	[Icon( "🌬" )]
	[Description( "Ex. Airsoft pellets" )]
	Mild = 1,
	[Icon( "🗡️" )]
	[Description( "Ex. Axe swing" )]
	Average = 2,
	[Icon( "💥" )]
	[Description( "Ex. Rifle bullet" )]
	Serious = 3,
	[Icon( "👼" )]
	[Description( "None of the above or heal" )]
	Nothing = 4
}

[Icon( "medication" )]
[Title( "Health" )]
public sealed class HealthComponent : Component
{
	/// <summary>
	/// What type to damage is able to ragdoll this
	/// </summary>
	[Property]
	public DamageType StunnedBy { get; set; } = DamageType.Nothing;

	/// <summary>
	/// Can this get damaged at all
	/// </summary>
	[Property]
	public bool Immortal { get; set; } = false;

	/// <summary>
	/// What type of damage is able to hurt this
	/// </summary>
	[Property]
	[HideIf( "Immortal", true )]
	public DamageType DamagedBy { get; set; } = DamageType.Mild;

	/// <summary>
	/// Should this get ragdolled as well when the damage is greater or equal than both DamagedBy and StunnedBy
	/// </summary>
	[Property]
	[HideIf( "Immortal", true )]
	public bool StunWhenDamaged { get; set; } = false;

	/// <summary>
	/// How many hit points this has, usually 1 hit point comes from DamageType.Mild
	/// </summary>
	[Property]
	[HideIf( "Immortal", true )]
	[Range( 1f, 100f, 1f, false )]
	public int MaxHealth { get; set; } = 10;

	[Sync]
	public int Health { get; set; }

	[Sync]
	public bool Alive { get; set; } = true;

	/// <summary>
	/// Can this thing regenerate health over time
	/// </summary>
	[Property]
	[HideIf( "Immortal", true )]
	public bool CanRegenerate { get; set; } = true;

	/// <summary>
	/// How much time passed since the last time its been damaged before it starts regenerating health
	/// </summary>
	[Property]
	[ShowIf( "CanRegenerate", true )]
	[HideIf( "Immortal", true )]
	[Range( 0f, 10f, 0.1f )]
	public float RegenerationTimer { get; set; } = 5f;

	/// <summary>
	/// How many seconds it takes to regenerate 1 hit point
	/// </summary>
	[Property]
	[ShowIf( "CanRegenerate", true )]
	[HideIf( "Immortal", true )]
	[Range( 0f, 5f, 0.1f )]
	public float RegenerationCooldown { get; set; } = 2f;

	/// <summary>
	/// What gets spawned (Preferably an item) when this dies (Not when it gets destroyed)
	/// </summary>
	[Property]
	public List<GameObject> DropOnDeath { get; set; }

	public delegate void AttackerInfo( int damage, DamageType type, GameObject attacker = null, Vector3 localHurtPosition = default, Vector3 forceDirection = default, float force = 0 );

	[Property]
	public AttackerInfo OnDamaged { get; set; }

	public TimeSince LastDamaged { get; set; }
	TimeUntil _nextHeal { get; set; }

	GameObject _bloodParticle;
	public List<ParticleEmitter> BloodEmitters = new();

	protected override void OnStart()
	{
		LastDamaged = 0;
		_nextHeal = 0;

		Health = MaxHealth;

		if ( PrefabLibrary.TryGetByPath( "prefabs/particles/blood_splat.prefab", out var prefabFile ) )
		{
			var prefab = SceneUtility.GetPrefabScene( prefabFile.Prefab ).Clone();

			if ( prefab != null )
			{
				prefab.Transform.World = GameObject.Transform.World;
				prefab.SetParent( GameObject );
				_bloodParticle = prefab;

				BloodEmitters = prefab.Components.GetAll<ParticleEmitter>( FindMode.EnabledInSelfAndDescendants ).ToList();

				_bloodParticle.Enabled = false;
			}
		}

	}

	/// <summary>
	/// How many hitpoints to remove (Negative will heal instead and not call the OnAttacked event)
	/// </summary>
	/// <param name="amount">The amount of damage dealth</param>
	/// <param name="type">The type of damage dealth</param>
	/// <param name="attacker">The person that attacked, null if not set</param>
	/// <param name="worldHurtPosition">The world position of where the damage happened, (0,0,0) if not set</param>
	/// <param name="forceDirection">The direction which the force will be applied, (0,0,0) if not set</param>
	/// <param name="force">How much force was behind that damage, 0 by default</param>
	public void Damage( int amount, DamageType type, GameObject attacker = null, Vector3 worldHurtPosition = default, Vector3 forceDirection = default, float force = 0 )
	{
		if ( !Alive ) return;
		if ( amount == 0 ) return;

		var stunned = type >= StunnedBy && type != DamageType.Nothing; // Don't ragdoll if we're healing
		var damaged = type >= DamagedBy;
		var localWorld = Transform.World.Position - worldHurtPosition;
		var realDirection = forceDirection != default ? forceDirection : localWorld.WithZ( 0f ).Normal;

		if ( !Immortal )
		{
			if ( damaged )
			{
				Health = Math.Clamp( Health - amount, 0, MaxHealth );

				if ( amount > 0 )
				{
					LastDamaged = 0; // We were just attacked
					_nextHeal = RegenerationTimer + RegenerationCooldown; // Reset the healtimer

					OnDamaged?.Invoke( amount, type, attacker, Transform.World.PointToLocal( worldHurtPosition ), realDirection, force );

					if ( _bloodParticle != null )
					{
						_bloodParticle.Enabled = true;
						_bloodParticle.Transform.Rotation = Rotation.Identity;
						_bloodParticle.Transform.Position = Transform.Position; // worldHurtPosition is like skewed or some shit

						foreach ( var emitter in BloodEmitters )
							emitter.ResetEmitter();
					}

					if ( Components.TryGet<NPC>( out var npc ) )
						npc.Damaged( attacker );
				}
			}
		}

		if ( stunned )
		{
			if ( damaged && !StunWhenDamaged && !Immortal ) return;

			var damageFrac = Math.Min( (float)amount / (float)MaxHealth, 1f );
			var ragdollTime = damageFrac * 5f;
			var ragdollVelocity = realDirection * force + Vector3.Up * force;

			if ( Components.TryGet<Player>( out var player ) )
			{
				player.MoveHelper?.Punch( ragdollVelocity );
				player.SetRagdoll( true, true, ragdollTime );
			}

			if ( Components.TryGet<NPC>( out var npc ) )
			{
				npc.LocalPunch( ragdollVelocity );
				npc.SetRagdoll( true, ragdollTime, 50f );
			}
		}

		if ( Health <= 0 )
		{
			Kill( attacker.Id );
			return;
		}
	}

	private void InternalKill( GameObject attacker = null )
	{
		Alive = false;

		if ( Components.TryGet<Player>( out var player ) )
			player.SetRagdoll( true, true, 9999999f );

		if ( Components.TryGet<NPC>( out var npc ) )
		{
			npc.SetRagdoll( true, 9999999f, 50f );

			npc.OnKilled?.Invoke( attacker );
		}

		if ( DropOnDeath != null )
		{
			foreach ( var item in DropOnDeath )
			{
				var droppedItem = item.Clone( Transform.Position + Vector3.Up * 30f, Transform.Rotation );
				droppedItem?.SetupNetworking();
			}
		}
	}

	/// <summary>
	/// Kill this
	/// </summary>
	/// <param name="attackerId"></param>
	[Broadcast]
	public void Kill( Guid attackerId )
	{
		var attackerObj = Game.ActiveScene.GetAllObjects( true )
			.Where( x => x.Id == attackerId )
			.FirstOrDefault();

		InternalKill( attackerObj );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Immortal && Alive )
		{
			if ( Health < MaxHealth )
			{
				if ( _nextHeal )
				{
					Damage( -1, DamageType.Nothing );
					_nextHeal = RegenerationCooldown;
				}
			}
		}
	}
}
