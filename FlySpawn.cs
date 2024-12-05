using Godot;
using System;
using System.Collections.Generic;


// Manages the activation, behavior, and navigation of fireflies
public partial class fly_spawn : StaticBody3D
{
	
	private PackedScene LeaderFly = GD.Load<PackedScene>("res://Utility_AI/lifeforms/nav_flies/leader_fly.tscn");

	private NavigationAgent3D navigationAgent3D;
	private VisibleOnScreenNotifier3D visibleOnScreenNotifier3D;
	private AudioStreamPlayer3D idleSounds;
	private AudioStreamPlayer3D activationSounds;

	// Properties for tweaking fly behavior
	[Export] private float spawnRadius = 0.25f;
	[Export] private float centralOffsetVariance = 0.15f;

	private List<Node3D> flies = new List<Node3D>();
	private float speed = 6.0f;

	private bool navigating = false;
	private float targetThreshold = 0.5f;

	private RandomNumberGenerator rng = new RandomNumberGenerator();
	private int randomizer;


	
    public override void _Ready()
    {
        rng.Randomize(); // Ensures randomization
        navigationAgent3D = GetNode<NavigationAgent3D>("NavigationAgent3D");
        visibleOnScreenNotifier3D = GetNode<VisibleOnScreenNotifier3D>("VisibleOnScreenNotifier3D");
        idleSounds = GetNode<AudioStreamPlayer3D>("idle_sounds");
        activationSounds = GetNode<AudioStreamPlayer3D>("activation_sound");

		SpawnFlies(); // Instances leader flies
		PlayIdleSounds(); // starts playing idle sounds
    }


	// Periodically plays idle sounds
	private async void PlayIdleSounds()
	{
		await ToSignal(GetTree().CreateTimer(rng.RandfRange(0.75f, 3.0f)), "timeout");
		randomizer = rng.RandiRange(1, 4);


		// Rarely plays a double click sound for extra variety
		if (randomizer == 4)
		{
			idleSounds.Play();
			await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
			idleSounds.Play();
		}
		else
		{
			idleSounds.Play();
		}
		PlayIdleSounds();
	}


	// Spawns a random number of leader flies
	private void SpawnFlies()
	{
		int randomFlyCount = rng.RandiRange(2, 4);
		for (int i = 0; i < randomFlyCount; i++)
		{
			var flyInstance = (Node3D)LeaderFly.Instantiate();
			flyInstance.GlobalTransform = new Transform3D(Basis.Identity, GetRandomSpawnPoint());
			AddChild(flyInstance);
			flies.Add(flyInstance);
		}
	}


	// Returns a random spawn location for a leader fly
	private Vector3 GetRandomSpawnPoint()
	{
		return new Vector3(
            rng.RandfRange(-spawnRadius, spawnRadius),
            rng.RandfRange(-spawnRadius, spawnRadius),
            rng.RandfRange(-spawnRadius, spawnRadius)
        );
    }


	// Enables/disables fly activity
	private void EnableFlies(bool active)
	{
		foreach (var fly in flies)
		{
			fly.SetProcess(active);
			fly.Visible = active;
		}
	}


	// Begins navigation to the fly's destination
	private void StartNavigation()
	{
		foreach(var fly in flies)
		{
			SetCollisionLayerValue(3, false);
			var targetPos = ((Node3D)GetTree().GetFirstNodeInGroup("dungeon_exit")).GlobalPosition;
			navigationAgent3D.TargetPosition = targetPos;
			navigating = true;
		}
	}



	// Gathers flies together to enhance navigation visuals
    private void GatherFlies()
	{
		foreach(var fly in flies)
		{
			var randomOffset = new Vector3(
				rng.RandfRange(-centralOffsetVariance, centralOffsetVariance),
				rng.RandfRange(-centralOffsetVariance, centralOffsetVariance) + 0.5f,
				rng.RandfRange(-centralOffsetVariance, centralOffsetVariance)
				);
			fly.Set("assemblyOffset", randomOffset);
			fly.Set("navigating", true);
			fly.Set("targetPosition", GlobalPosition + randomOffset);
			fly.Set("reachingCentralPoint", true);

			fly.Connect("reached_target", new Callable(this, nameof(OnFlyReachedCentralPoint)));
		}
	}


	// Called when a fly is gathered
	private void OnFlyReachedCentralPoint()
	{
		if (AllFliesReady())
		{
			StartNavigation();
		}
	}



	// Returns true when all flies are gathered
	private bool AllFliesReady()
	{
		foreach (var fly in flies)
		{
			if ((bool)fly.Get("ReachingCentralPoint"))
			{
				return false;
			}
		}
		activationSounds.Play(); // plays a sound to signal they're navigating
		return true;
	}


	// Movement logic of navigation
    public override void _PhysicsProcess(double delta)
    {
		if (navigationAgent3D.IsNavigationFinished())
			return;

		var nextPos = navigationAgent3D.GetNextPathPosition();
		var direction = (nextPos - GlobalPosition).Normalized();
		var movement = direction * (float)speed * (float)delta;

		GlobalPosition += movement;

		foreach (var fly in flies)
		{
			fly.Call("SetNavPos", GlobalPosition); // Updates position of each leader fly
		}
    }


	// Enables flies when on screen
	private void OnVisibleOnScreenNotifier3DScreenEntered()
	{
		EnableFlies(true);
	}


	// Disables flies when off screen
    private void OnVisibleOnScreenNotifier3DScreenExited()
    {
        if (!navigating)
		{
            EnableFlies(false);
        }
    }


	// Begins navigation on player interact
	public void PlayerInteract()
	{
		activationSounds.Play();
		GatherFlies();
	}


	// Ends navigation
	private void OnNavigationAgent3DNavigationFinished()
	{
		navigating = false;
		foreach (var fly in flies)
		{
			fly.Call("ResumeFlight");
		}
	}
}
