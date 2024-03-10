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
	
	public static LegacyParticles Create( string path, Transform transform = default, GameObject parent = null, List<ParticleControlPoint> controlPoints = null, int? deleteTime = null )
	{
		var obj = Game.ActiveScene.CreateObject();
		obj.Transform.World = transform;
		obj.Name = $"Legacy Particles";

		if ( parent != null ) obj.Parent = parent;

		var particle = new LegacyParticles()
		{
			GameObject = obj,
			System = obj.Components.Create<LegacyParticleSystem>(),
			Transform = transform
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

		if ( deleteTime != null )
			new Action( async () =>
			{
				await GameTask.Delay( deleteTime.Value );
				particle?.Destroy();
			} ).Invoke();

		return particle;
	}

	public void replay()
	{
		System.Enabled = false;
		System.Enabled = true;
	}

	public void Destroy()
	{
		System?.Destroy();
		GameObject?.Destroy();
	}

	private void AddControlPoint( int index, ParticleControlPoint cp )
	{
		if ( System.ControlPoints.Count < index + 1 )
			System.ControlPoints.Insert( index, cp );
		else
			System.ControlPoints[index] = cp;
	}

	public void SetGameObject( int index, GameObject obj )
	{
		var cp = new ParticleControlPoint() 
		{ 
			Value = ParticleControlPoint.ControlPointValueInput.GameObject, 
			GameObjectValue = obj
		};

		AddControlPoint( index, cp );
		System.SceneObject.SetControlPoint( index, Transform );
	}

	public void SetVector( int index, Vector3 vector )
	{
		var cp = new ParticleControlPoint() 
		{ 
			Value = ParticleControlPoint.ControlPointValueInput.Vector3, 
			VectorValue = vector
		};

		AddControlPoint( index, cp );
		System.SceneObject.SetControlPoint( index, vector );
	}

	public void SetFloat( int index, float @float )
	{
		var cp = new ParticleControlPoint() 
		{ 
			Value = ParticleControlPoint.ControlPointValueInput.Float, 
			FloatValue = @float
		};

		AddControlPoint( index, cp );
		System.SceneObject.SetControlPoint( index, @float );
	}
}
