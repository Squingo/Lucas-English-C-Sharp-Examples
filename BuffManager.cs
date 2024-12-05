using Godot;
using System;
using System.Collections.Generic;
using Depthseeker.UI;


// Handles activation/deactivation of various buffs and their UI components
public partial class BuffManager : Control
{
    private Control buffManagementUi;

    private TimerManager timerManager;

    private List<BuffSlot> buffSlots = new List<BuffSlot>();

    // Corresponding buff texture coordinates
    private Dictionary<string, Vector2> buffAtlasType = new Dictionary<string, Vector2>
    {
        { "geode", new Vector2(0, 0) },
        { "speed", new Vector2(21, 0) },
        { "jump_boost", new Vector2(42, 18) },
        { "fiend", new Vector2(21, 36) },
        { "symbiosis", new Vector2(63, 0) },
        { "appraisal", new Vector2(21, 18) },
        { "hyper", new Vector2(63, 18) },
        { "exploit", new Vector2(0, 18) }
    };


    // Used to see a buff's status
    private Dictionary<string, bool> activeBuffSlots = new Dictionary<string, bool>
    {
        { "geode", false },
        { "speed", false },
        { "jump_boost", false },
        { "fiend", false },
        { "symbiosis", false },
        { "appraisal", false },
        { "hyper", false },
        { "exploit", false }
    };


    public override void _Ready()
    {
        // reference scene components
        buffManagementUi = GetNode<Control>(".");
        timerManager = GetNode<TimerManager>("Timer_Manager");


        foreach (Node child in buffManagementUi.GetChildren())
        {
            if (child.IsInGroup("buff_slot)"))
            {
                BuffSlot buffSlot = (BuffSlot)child;
                buffSlots.Add(buffSlot);
                buffSlot.BuffManager = this;
            }
        }
    }


    // Activates and starts a timer for the specified buff
    public void TriggerBuff(string buffType, float duration)
    {
        if (buffAtlasType.ContainsKey(buffType))
        {
            BuffSlot chosenBuffSlot = null;


            // extends buff duration if already active
            if (activeBuffSlots[buffType])
            {
                foreach (BuffSlot buffSlot in buffSlots)
                {
                    if (buffSlot.BuffType == buffType)
                    {
                        // Extends time
                        float remainingTime = timerManager.ReturnRemainingTime(buffSlot.Name);
                        float newTime = duration + remainingTime;
                        timerManager.SetTimer(buffSlot.Name, newTime, new Callable(buffSlot, "BuffFinished"));


                        // Sets a timer to show when a buff is low
                        if (newTime > 5)
                        {
                            float newShakeTime = duration - 3;
                            string shakeName = buffSlot.Name + "shake";
                            timerManager.SetTimer(shakeName, newShakeTime, new Callable(buffSlot, "BuffRunningLow"));
                        }
                    }
                }
            }
            else
            {
                // Activates a buff in an open slot
                foreach (BuffSlot buffSlot in buffSlots)
                {
                    if (!buffSlot.Active)
                    {
                        buffSlot.Active = true;
                        chosenBuffSlot = buffSlot;
                        break;
                    }
                }

                if (chosenBuffSlot != null)
                {
                    activeBuffSlots[buffType] = true;
                    Vector2 rectCoords = buffAtlasType[buffType];
                    chosenBuffSlot.SetBuff(rectCoords, buffType);
                    timerManager.SetTimer(chosenBuffSlot.Name, duration, new Callable(chosenBuffSlot, "BuffFinished"));


                    if (duration > 5)
                    {
                        float shakeTime = duration - 3;
                        string shakeName = chosenBuffSlot.Name + "shake";
                        timerManager.SetTimer(shakeName, shakeTime, new Callable(chosenBuffSlot, "BuffRunningLow"));
                    }
                }
            }
        }
    }


    // Activates a buff that expires conditionally
    public void TriggerConditionalBuff(string buffType)
    {
        if (buffAtlasType.ContainsKey(buffType))
        {
            BuffSlot chosenBuffSlot = null;


            // activates the buff if inactive
            if (!activeBuffSlots[buffType])
            {
                foreach (BuffSlot buffSlot in buffSlots)
                {
                    if (!buffSlot.Active)
                    {
                        buffSlot.Active = true;
                        chosenBuffSlot = buffSlot;
                        break;
                    }
                }

                if (chosenBuffSlot != null)
                {
                    activeBuffSlots[buffType] = true;
                    Vector2 rectCoords = buffAtlasType[buffType];
                    chosenBuffSlot.SetBuff(rectCoords, buffType);
                }
            }
        }
    }


    // removes the specified buff and clears related timers
    public void ClearSpecifiedBuff(string buffType)
    {
        if (activeBuffSlots[buffType])
        {
            foreach (BuffSlot buffSlot in buffSlots)
            {
                if (buffSlot.BuffType == buffType)
                {
                    timerManager.ClearTimer(buffSlot.Name);
                    string shakeName = buffSlot.Name + "shake";
                    timerManager.ClearTimer(shakeName);
                    buffSlot.BuffFinished();
                }
            }
        }
    }


    public void SetBuffInactive(string buffType)
    {
        activeBuffSlots[buffType] = false;
    }
}