using Sandbox;

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

		var trace = Trace.Ray( pawn.ViewRay, 500f )
			.Ignore( pawn )
			.WithoutTags( "trigger" )
			.Size( model.Bounds )
			.Run();

		var entity = new ModelEntity();
		entity.Model = model;
		entity.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		entity.Position = trace.EndPosition;
	}

	[ConCmd.Server( "delete_entity" )]
	public static void DeleteEntity()
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		var trace = Trace.Ray( pawn.ViewRay, 500f )
			.Ignore( pawn )
			.WithoutTags( "trigger" )
			.Run();

		if ( trace.Entity == null || trace.Entity is WorldEntity )
		{
			Log.Error( $"Didn't find any entity to delete." );
			return;
		}

		trace.Entity?.Delete();

	}

	[ConCmd.Server( "spawn_entity" )]
	public static void SpawnEntity( string typeName )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		var type = GlobalGameNamespace.TypeLibrary.GetType( typeName );
		if ( !type.IsValid )
		{
			Log.Error( $"Couldn't find the type: {typeName}" );
			return;
		}

		var trace = Trace.Ray( pawn.ViewRay, 500f )
			.Ignore( pawn )
			.WithoutTags( "trigger" )
			.Run();

		var entity = type.Create<Entity>();
		if ( entity == null )
		{
			Log.Error( $"Failed to create the entity: {typeName}" );
			return;
		}

		entity.Position = trace.EndPosition;
	}

	[ConCmd.Server( "give_exp" )]
	public static void GiveExperience( int amount )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		pawn.Experience += amount;
	}

	[ConCmd.Server( "add_effect" )]
	public static void AddEffect( string effect, float duration = 5f, int stacks = 1 )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		var effectType = GlobalGameNamespace.TypeLibrary.GetType( effect );
		if ( effectType == null || !effectType.TargetType.IsSubclassOf( typeof ( BaseEffect ) ) )
		{
			Log.Error( $"Failed to find the effect: {effect}" );
			return;
		}

		pawn.Effects.Apply( effectType.TargetType, duration, stacks );
	}

	[ConCmd.Admin( "devcam" )]
	public static void DeveloperCamera()
	{
		var cl = ConsoleSystem.Caller;
		if ( !cl.IsValid )
			return;

		var camera = cl.Components.Get<DevCamera>( true );
		if ( camera == null )
		{
			camera = new DevCamera();
			cl.Components.Add( camera );

			return;
		}

		camera.Enabled = !camera.Enabled;
	}
}
