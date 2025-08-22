using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUp
{
    private List<PowerUp> powerUpsAll;
    LevelUp()
    {
        powerUpsAll = new List<PowerUp>();
        powerUpsAll.Add(new PowerUp("Health", @"LevelIp/Images/Health.png", "Increase health", "Health"));
        powerUpsAll.Add(new PowerUp("Damage", @"LevelIp/Images/Damage.png", "Increase damage", "Damage"));
        powerUpsAll.Add(new PowerUp("Speed", @"LevelIp/Images/Speed.png", "Increase speed", "Speed"));
        powerUpsAll.Add(new PowerUp("Armor", @"LevelIp/Images/Armor.png", "Increase armor", "Armor"));
        powerUpsAll.Add(new PowerUp("MoreBullets", @"LevelIp/Images/MoreBullets.png", "More bullets", "Jump"));

    }
    PowerUp GetRandomPowerUp()
    {
        return powerUpsAll[Random.Range(0, powerUpsAll.Count)];
    }
    PowerUp AssignRandomPowerUpLevel(PowerUp powerUp)
    {
        int luck = Random.Range(0, 100);
        if (luck < 20)
        {
            powerUp.level = UpgradeLevel.Grey;
        }
        else if (luck < 65)
        {
            powerUp.level = UpgradeLevel.Green;
        }
        else if (luck < 85)
        {
            powerUp.level = UpgradeLevel.Blue;
        }
        else if (luck < 95)
        {
            powerUp.level = UpgradeLevel.Purple;
        }
        else
        {
            powerUp.level = UpgradeLevel.Orange;
        }

        if (powerUp.level == UpgradeLevel.Grey)
        {
            powerUp.valuePct = 5;
        }
        else if (powerUp.level == UpgradeLevel.Green)
        {
            powerUp.valuePct = 10;
        }
        else if (powerUp.level == UpgradeLevel.Blue)
        {
            powerUp.valuePct = 15;
        }
        else if (powerUp.level == UpgradeLevel.Purple)
        {
            powerUp.valuePct = 30;
        }
        else if (powerUp.level == UpgradeLevel.Orange)
        {
            powerUp.valuePct = 50;
        }
        return powerUp;
    }
}
