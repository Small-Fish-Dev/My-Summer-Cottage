namespace Sauna;

public class Bobber : Component
{
	public LeFisheSpawner CurrentSpawner { get; set; }
	public FishingRod Rod { get; set; }
	public Rigidbody Rigidbody { get; private set; }

	private SphereCollider _collider;

	protected override void OnAwake()
	{
		Rigidbody = Components.Get<Rigidbody>();
		_collider = Components.Get<SphereCollider>();
	}

	public void PullOut()
	{
		if ( CurrentSpawner.IsValid() )
			CurrentSpawner.PullOutFish( GameObject );
	}
}
