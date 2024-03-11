using Sandbox;

[Icon( "groups" )]
public sealed class NpcSpawnArea : Component
{
	public struct NpcChance
	{
		[Property]
		[JsonInclude]
		public GameObject Npc { get; set; }

		[Property]
		[JsonInclude]
		[Range( 0.01f, 1f, 0.01f )]
		public float SpawnChance { get; set; } = 0.2f;

		public NpcChance() { }
	}
	[Property]
	public float Radius { get; set; }

	[Property]
	public float Height { get; set; }

	[Property]
	[JsonInclude]
	public List<NpcChance> NpcPool { get; set; }

	[JsonIgnore]
	public List<GameObject> SpawnedNpcs { get; set; } = new();

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		var draw = Gizmo.Draw;

		draw.LineCylinder( Vector3.Down * Height / 2f, Vector3.Up * Height / 2f, Radius, Radius, 15 );
	}

	protected override void OnStart()
	{
	}

	public void SpawnNPCs()
	{
		RemoveNPCs();

		foreach ( var npc in NpcPool )
		{
			var random = Game.Random.Float( 0f, 1f );
			var shouldSpawn = random <= npc.SpawnChance;

			if ( shouldSpawn )
			{
				var tries = 0;

				while ( tries <= 10 )
				{
					var randomDirection = Rotation.FromYaw( Game.Random.Float( 360 ) ).Forward;
					var randomPosition = Transform.Position + randomDirection * Game.Random.Float( Radius );
					var startPos = randomPosition.WithZ( Transform.Position.z + Height / 2f );
					var endPos = randomPosition.WithZ( Transform.Position.z - Height / 2f );

					var groundTrace = Game.ActiveScene.Trace.Ray( startPos, endPos )
						.Size( 50f )
						.WithoutTags( "player", "npc", "trigger" )
						.Run();

					if ( groundTrace.Hit && !groundTrace.StartedSolid )
					{
						if ( Vector3.GetAngle( Vector3.Up, groundTrace.Normal ) <= 60f )
						{
							var clone = npc.Npc.Clone( groundTrace.HitPosition );

							if ( clone != null )
								SpawnedNpcs.Add( clone );

							break;
						}
					}

					tries++;
				}
			}
		}
	}

	public void RemoveNPCs()
	{
		foreach ( var npc in SpawnedNpcs )
		{
			npc?.Destroy();
		}
	}
}
