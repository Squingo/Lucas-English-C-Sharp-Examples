using Godot;
using System;
using System.Collections.Generic;


// Manages timers for clean startup and clearing
public partial class TimerManager : Node
{
    // Stores the timers by name
    private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();


    // Creates a non-repeating timer that calls a function on timeout
    public void SetTimer(string timerName, float length, Callable callback)
    {
        // Clears a timer if it already exists
        if (timers.ContainsKey(timerName))
        {
            timers[timerName].Stop();
            timers[timerName].QueueFree();
        }


        // Creates and configures the timer
        Timer timer = new Timer
        {
            WaitTime = length,
            OneShot = true
        };
        AddChild(timer);
        timers[timerName] = timer;

        timer.Connect("timout", callback); // Connects the function to call on timeout
        timer.Start();
    }


    // Creates a repeating timer that calls a function on timeout
    public void SetRepeatingTimer(string timerName, float length, Callable callback)
    {
        // Clears a timer if it already exists
        if (timers.ContainsKey(timerName))
        {
            timers[timerName].Stop();
            timers[timerName].QueueFree();
        }


        // Creates and configures the timer
        Timer timer = new Timer
        {
            WaitTime = length,
            OneShot = false
        };
        AddChild(timer);
        timers[timerName] = timer;

        timer.Connect("timout", callback);
        timer.Start();
    }


    // Adds time to an existing timer
    public void AddTime(string timerName, float additionalTime)
    {
        if (timers.ContainsKey(timerName))
        {
            float newTime = (float)timers[timerName].TimeLeft + additionalTime;
            timers[timerName].Stop();
            timers[timerName].Start(newTime);
        }
    }


    // Returns the remaining time of a specified timer
    public float ReturnRemainingTime(string timerName)
    {
        if (timers.ContainsKey(timerName))
        {
            return (float)timers[timerName].TimeLeft;
        }
        return -1;
    }


    // Clears a specified timer
    public void ClearTimer(string timerName)
    {
        if (timers.ContainsKey(timerName))
        {
            timers[timerName].Stop();
            timers[timerName].QueueFree();
            timers.Remove(timerName);
        }
    }


    public override void _ExitTree()
    {
        ClearAllTimers();
    }


    // Ensures timers are cleared after removal from the scene
    private void ClearAllTimers()
    {
        foreach (var kvp in timers)
        {
            Timer timer = kvp.Value;
            if (!timer.IsStopped())
            {
                timer.Stop();
            }
            timer.QueueFree();
        }
        timers.Clear();
    }
}


    