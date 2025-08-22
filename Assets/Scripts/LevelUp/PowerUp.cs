using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single power up object
/// </summary>
public class PowerUp
{
    public PowerUp(string caption, string picturePath, string description, string affectedStat)
    {
        this.caption = caption;
        this.picturePath = picturePath;
        this.description = description;
        this.affectedStat = affectedStat;
    }
    string caption;
    string picturePath;
    string description;
    string affectedStat;
    public int valuePct;
    public UpgradeLevel level;
}
public enum UpgradeLevel
{
    Grey,
    Green,
    Blue,
    Purple,
    Orange
}