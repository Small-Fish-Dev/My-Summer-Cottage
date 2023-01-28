namespace Sauna;

partial class Sauna
{
	[ConCmd.Server( "spawn_model" )]
	public static void SpawnModel( string path )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn ) 
			return;

		var model = Model.Load( path );
		if ( model == null || model.Name == "models/dev/error.vmdl" )
		{
			Log.Error( $"Couldn't load the model: {path}" );
			return;
		}

		var ray = new Ray(
			pawn.Position + Vector3.Up * pawn.CollisionBox.Maxs.z,
			pawn.ViewAngles.ToRotation().Forward );

		var trace = Trace.Ray( ray, 500f )
			.Ignore( pawn )
			.Size( model.Bounds )
			.Run();

		var entity = new ModelEntity();
		entity.Model = model;
		entity.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		entity.Position = trace.EndPosition;
	}

	[ConCmd.Server( "give_exp" )]
	public static void GiveExperience( int amount )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		pawn.Experience += amount;
	}
}
