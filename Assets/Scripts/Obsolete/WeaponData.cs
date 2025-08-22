using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponData
{
    public string Name { get; set; }
    public string PrefabName { get; set; }
    /// <summary>
    /// Name of settings file
    /// </summary>
    public string SettingsName { get; set; }
    public float Damage { get; set; }
    public float DamageFluctuation { get; set; }
    public float ClipSize { get; set; }
    public float Precision { get; set; }
    public float ReloadTime { get; set; }
    /// <summary>
    /// Shotgun 8gauge, Bird
    /// </summary>
    public float SimultaniousBullets { get; set; }
    public float CritChance { get; set; }
    /// <summary>
    /// Stagger, how much staggers enemy on hit
    /// </summary>
    public float Stagger { get; set; }
    /// <summary>
    /// How much does itinfluence next shot pistol malo, AK mnogo
    /// </summary>
    public float Recoil { get; set; }
    public float Weight { get; set; }

    void Start()
    {

    }



}
