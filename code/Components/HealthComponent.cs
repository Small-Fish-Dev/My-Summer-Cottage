using Sandbox;
using Sauna;

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
	[HideIf( "Immortal", true )]
	[Range( 0f, 10f, 0.1f )]
	public float RegenerationTimer { get; set; } = 5f;

	/// <summary>
	/// How many seconds it takes to regenerate 1 hit point
	/// </summary>
	[Property]
	[HideIf( "Immortal", true )]
	[Range( 0f, 5f, 0.1f )]
	public float RegenerationCooldown { get; set; } = 2f;

	public delegate void AttackerInfo( int damage, DamageType type, GameObject attacker = null, Vector3 localHurtPosition = default, Vector3 forceDirection = default, float force = 0 );

	public AttackerInfo OnAttacked { get; set; }

	public TimeSince LastDamaged { get; set; }
	TimeUntil _nextHeal { get; set; }

	protected override void OnStart()
	{
		LastDamaged = 0;
		_nextHeal = 0;

		Health = MaxHealth;
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
	[Broadcast]
	public void Damage( int amount, DamageType type, GameObject attacker = null, Vector3 worldHurtPosition = default, Vector3 forceDirection = default, float force = 0 )
	{
		if ( Immortal ) return;
		if ( amount == 0 ) return;

		var stunned = type >= StunnedBy && type != DamageType.Nothing; // Don't ragdoll if we're healing
		var damaged = type >= DamagedBy;

		if ( damaged )
		{
			Health = Math.Clamp( Health - amount, 0, MaxHealth );

			if ( amount > 0 )
			{
				LastDamaged = 0; // We were just attacked
				_nextHeal = RegenerationTimer + RegenerationCooldown; // Reset the healtimer

				OnAttacked?.Invoke( amount, type, attacker, worldHurtPosition == default ? worldHurtPosition : Transform.World.PointToLocal( worldHurtPosition ), forceDirection, force );
			}
		}

		if ( stunned )
		{
			if ( damaged && !StunWhenDamaged ) return;

			var damageFrac = amount / MaxHealth;
			var ragdollTime = damageFrac * 10f;
			var ragdollVelocity = forceDirection * force + Vector3.Up * 50;

			if ( Components.TryGet<Player>( out var player ) )
			{
				player.SetRagdoll( true, true, ragdollTime );
				player.MoveHelper?.Punch( ragdollVelocity );
			}

			if ( Components.TryGet<NPC>( out var npc ) )
			{
				npc.SetRagdoll( true, ragdollTime, 50f );
				npc.LocalPunch( ragdollVelocity );
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !Immortal )
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
