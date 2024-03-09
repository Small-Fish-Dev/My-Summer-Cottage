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
	public int Health { get; set; } = 10;

	protected override void OnUpdate()
	{

	}
}
