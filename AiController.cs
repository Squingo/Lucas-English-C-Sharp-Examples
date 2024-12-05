using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AI_Behaviors;


// Handles registering, determining, and executing a lifeform's behavior based on utility
public partial class ai_controller : Node3D
{
    // Exported properties for assigning behaviors and settings in the editor
    [Export] public List<AI_Behavior> Behaviors { get; set; } = new List<AI_Behavior>();
    [Export] public AI_Behavior CurrentBehavior { get; set; } = null;
    [Export] public float UtilityCalculationInterval { get; set; } = 0.5f;

    // Internal variables for managing behaviors and utility evaluation
    private Timer _utilityTimer;
    private AI_Behavior _bestBehavior = null;
    private List<AI_Behavior> _registeredBehaviors = new List<AI_Behavior>();



    // Called when the node enters the scene tree
    public override void _Ready()
    {
        SetPhysicsProcess(true);

        // Randomizes the initial calculation start time to prevent calculations on the same tick
        float randomDelay = GD.Randf() % 1.0f;
        GetTree().CreateTimer(randomDelay).Timeout += () =>
        {
            // Initialize a utility timer for regular utility calculation
            _utilityTimer = new Timer
            {
                WaitTime = UtilityCalculationInterval,
                OneShot = false
            };
            _utilityTimer.Timeout += OnTimerTimeout;
            AddChild(_utilityTimer);
            _utilityTimer.Start();
        };
    }


    // Registers a lifeform's behaviors in its AI controller and initializes them with its properties - WIP
    public void RegisterBehaviors(CharacterBody3D owner)
    {
        foreach (var behavior in Behaviors)
        {
            var behaviorInstance = (AI_Behavior)behavior.script;

            // Copies behavior properties from the template to the new instance
            foreach (var property in behavior.GetPropertyList())
            {
                if (behaviorInstance.HasMethod(property.name))
                {
                    behaviorInstance.Set(property.name, behavior.Get(property.name));
                }
            }
            // Sets the behavior's owner and lifeform type
            behaviorInstance.SetOwner(owner, behavior.lifeformType);
            _registeredBehaviors.Add(behaviorInstance);
        }
    }


    // Runs every physics frame to update and perform the current behavior
    public override void _PhysicsProcess(double delta)
    {
        if (_bestBehavior != CurrentBehavior)
        {
        
            if (CurrentBehavior != null)
            {
                CurrentBehavior.isActive = false;
                CurrentBehavior.StopBehavior();
                CurbBehavior(CurrentBehavior);
            }

            CurrentBehavior = _bestBehavior;

            if (CurrentBehavior != null)
            {
                CurrentBehavior.isActive = true;
                CurrentBehavior.StartBehavior();
            }
        }

        CurrentBehavior?.PerformBehavior((float)delta);
    }


    // Selects the best behavior on timeout
    private void OnTimerTimeout()
    {
        _bestBehavior = GetBestBehavior();
    }


    // Runs calculations and determines the best behavior
    private AI_Behavior GetBestBehavior()
    {
        AI_Behavior greatestBehavior = null;
        float bestUtility = 0.0f;

        foreach (var behavior in _registeredBehaviors)
        {
            float utility = behavior.CalculateUtility();
            if (utility > bestUtility)
            {
                bestUtility = utility;
                greatestBehavior = behavior;
            }
        }

        return greatestBehavior;
    }


    // When a behavior ends, adds a cooldown or calls a unique override method (the crunchers needed to calm down)
    public async void CurbBehavior(AI_Behavior behavior)
    {
        if (behavior.HasMethod("CurbOverride"))
        {
            behavior.Call("CurbOverride");
        }
        else
        {
            behavior.curbed = true;
            // Waits for a random time between the cd range
            float curbDuration = (float)GD.RandRange(behavior.curbPeriodMin, behavior.curbPeriodMax);
            await ToSignal(GetTree().CreateTimer(curbDuration), "timeout");
            behavior.curbed = false;
        }
    }


    // Forces a specific behavior when manually called
    public void ForceBehavior(AI_Behavior behavior)
    {
        _bestBehavior = behavior;
    }
}
