/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Thanks!

               unitycodemonkey.com
    --------------------------------------------------
 */

using Assets.Scripts.HealthSystem;
using CodeMonkey.HealthSystemCM;
using System;
using UnityEngine;

/// <summary>
/// Health System: Damage, Heal, fires several events when data changes.
/// Use on Units, Buildings, Items; anything you want to have some health
/// Use HealthSystemComponent if you want to add a HealthSystem directly to a Game Object instead of using the C# constructor
/// </summary>
public class HealthSystemArmour
{

    public event EventHandler OnHealthChanged;
    public event EventHandler OnHealthMaxChanged;
    public event EventHandler OnDamaged;
    public event EventHandler OnHealed;
    public event EventHandler OnDead;

    //private float healthMax;
    //private float health;
    //private float armourMax;
    //private float armour;
    public Health health;

    /// <summary>
    /// Construct a HealthSystem, receives the health max and sets current health to that value
    /// </summary>
    public HealthSystemArmour(float healthMax, float armourMax)
    {
        Health health = new Health(healthMax, armourMax);
        health.healthMax = healthMax;
        health.armourMax = armourMax;
    }

    /// <summary>
    /// Get the current health
    /// </summary>
    public Health GetHealth()
    {
        return health;
    }


}






