namespace Sauna.SFX;

public class LegacyParticles
{
	public GameObject GameObject { get; private set; }
	public LegacyParticleSystem System { get; private set; }

	public Transform Transform
	{
		get => GameObject.Transform.World;
		set => GameObject.Transform.World = value;
	}

	private LegacyParticles() { }
	
	public static LegacyParticles Create( string path, Transform transform = default, List<ParticleControlPoint> controlPoints = null )
	{
		var obj = Game.ActiveScene.CreateObject();
		obj.Transform.World = transform;

		var particle = new LegacyParticles()
		{
			GameObject = obj,
			System = obj.Components.Create<LegacyParticleSystem>()
		};

		particle.System.Particles = ParticleSystem.Load( path );
		particle.System.ControlPoints = controlPoints ?? new List<ParticleControlPoint>()
		{
			new ParticleControlPoint() 
			{ 
				Value = ParticleControlPoint.ControlPointValueInput.Vector3, 
				VectorValue = transform.Position 
			}
		};

		return particle;
	}

	public void Destroy()
	{
		System?.Destroy();
		GameObject?.Destroy();
	}

	public void SetGameObject( int index, GameObject obj )
	{
		var cp = new ParticleControlPoint() 
		{ 
			Value = ParticleControlPoint.ControlPointValueInput.GameObject, 
			GameObjectValue = obj
		};

		System.ControlPoints.Insert( index, cp );
		System.SceneObject.SetControlPoint( index, Transform );
	}

	public void SetVector( int index, Vector3 vector )
	{
		var cp = new ParticleControlPoint() 
		{ 
			Value = ParticleControlPoint.ControlPointValueInput.Vector3, 
			VectorValue = vector
		};

		System.ControlPoints.Insert( index, cp );
		System.SceneObject.SetControlPoint( index, vector );
	}

	public void SetFloat( int index, float @float )
	{
		var cp = new ParticleControlPoint() 
		{ 
			Value = ParticleControlPoint.ControlPointValueInput.Float, 
			FloatValue = @float
		};

		System.ControlPoints.Insert( index, cp );
		System.SceneObject.SetControlPoint( index, @float );
	}
}
