using Sandbox;
using Sauna;

[Icon( "category" )]
public sealed class ItemSpawnArea : Component
{
	public struct ItemAmount
	{
		[Property]
		[JsonInclude]
		public GameObject Item { get; set; }

		[Property]
		[JsonInclude]
		[Range( 1, 100, 1 )]
		public float AmountToSpawn { get; set; } = 5;

		[Property]
		[JsonInclude]
		public bool StartFrozen { get; set; } = true;

		public ItemAmount() { }
	}

	[Property]
	public float Radius { get; set; }

	[Property]
	public float Height { get; set; }

	[Property]
	[JsonInclude]
	public List<ItemAmount> ItemPool { get; set; }

	public struct ItemPosition
	{
		public Vector3 SpawnPosition;
		public GameObject Item;

		public ItemPosition() { }
		public ItemPosition( Vector3 position, GameObject item )
		{
			SpawnPosition = position;
			Item = item;
		}
	}

	[JsonIgnore]
	[Sync]
	public NetList<ItemPosition> SpawnedItems { get; set; } = new();

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		var draw = Gizmo.Draw;

		draw.LineCylinder( Vector3.Down * Height / 2f, Vector3.Up * Height / 2f, Radius, Radius, 15 );
	}

	protected override void OnStart()
	{
	}

	public void SpawnItems()
	{
		RemoveItems();

		foreach ( var item in ItemPool )
		{
			for ( int i = 0; i < item.AmountToSpawn; i++ )
			{
				var tries = 0;

				while ( tries <= 10 )
				{
					var randomDirection = Rotation.FromYaw( Game.Random.Float( 360f ) ).Forward;
					var randomPosition = Transform.Position + randomDirection * Game.Random.Float( Radius );
					var startPos = randomPosition.WithZ( Transform.Position.z + Height / 2f );
					var endPos = randomPosition.WithZ( Transform.Position.z - Height / 2f );

					var groundTrace = Game.ActiveScene.Trace.Ray( startPos, endPos )
						.Size( 5f )
						.WithoutTags( "player", "npc", "trigger" )
						.Run();

					if ( groundTrace.Hit && !groundTrace.StartedSolid )
					{
						if ( Vector3.GetAngle( Vector3.Up, groundTrace.Normal ) <= 60f )
						{
							var clone = item.Item.Clone( groundTrace.HitPosition - Vector3.Up * 2.5f, Rotation.FromYaw( Game.Random.Float( 360f ) ) );
							clone.NetworkMode = NetworkMode.Object;
							clone.NetworkSpawn();

							if ( clone != null )
							{
								if ( item.StartFrozen )
									if ( clone.Components.TryGet<Rigidbody>( out var body ) )
										body.MotionEnabled = false;

								SpawnedItems.Add( new ItemPosition( groundTrace.HitPosition, clone ) );
							}

							break;
						}
					}

					tries++;
				}
			}
		}
	}

	public void RemoveItems()
	{
		foreach ( var item in SpawnedItems )
		{
			if ( !item.Item.IsValid() ) continue;

			if ( item.Item.Transform.Position.Distance( item.SpawnPosition ) < 10f )
				item.Item?.Destroy();
		}

		SpawnedItems.Clear();
	}
}
