using Sandbox;

public enum DamageType
{
	[Icon( "🌬" )]
	[Description( "Ex. Airsoft pellets" )]
	Mild,
	[Icon( "🗡️" )]
	[Description( "Ex. Axe swing" )]
	Average,
	[Icon( "💥" )]
	[Description( "Ex. Rifle bullet" )]
	Serious,
	[Icon( "👼" )]
	[Description( "None of the above or nothing at all" )]
	Nothing
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
	/// How many hit points this has, usually 1 hit point comes from DamageType.Mild
	/// </summary>
	[Property]
	[Range( 1f, 100f, 1f, false )]
	public int MaxHealth { get; set; } = 10;

	[Sync]
	public int Health { get; set; }

	/// <summary>
	/// Can this thing regenerate health over time
	/// </summary>
	[Property]
	public bool CanRegenerate { get; set; } = true;

	/// <summary>
	/// How much time passed since the last time its been damaged before it starts regenerating health
	/// </summary>
	[Property]
	[Range( 0f, 10f, 0.1f )]
	public float RegenerationTimer { get; set; } = 5f;

	/// <summary>
	/// How many seconds it takes to regenerate 1 hit point
	/// </summary>
	[Property]
	[Range( 0f, 5f, 0.1f )]
	public float RegenerationCooldown { get; set; } = 2f;

	public TimeSince LastDamaged { get; set; }
	TimeUntil _nextHeal { get; set; }

	protected override void OnStart()
	{
		LastDamaged = 0;
		_nextHeal = 0;

		Health = MaxHealth;

		Damage( 4 );
	}

	/// <summary>
	/// How many hitpoints to remove (Negative will heal instead and not call the OnAttacked event)
	/// </summary>
	/// <param name="amount"></param>
	/// <param name="attacker"></param>
	/// <param name="hurtPosition"></param>
	[Broadcast]
	public void Damage( int amount, GameObject attacker = null, Vector3 hurtPosition = default )
	{
		if ( amount == 0 ) return;

		var newHealth = Math.Clamp( Health - amount, 0, MaxHealth );

		Health = newHealth;

		if ( amount > 0 )
		{
			LastDamaged = 0; // We were just attacked
			_nextHeal = RegenerationTimer + RegenerationCooldown; // Reset the healtimer
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( Health < MaxHealth )
		{
			if ( _nextHeal )
			{
				Damage( -1 );
				_nextHeal = RegenerationCooldown;
			}
		}
	}
}
