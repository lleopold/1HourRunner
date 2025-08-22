using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.HealthSystem
{
    public class Health : MonoBehaviour
    {
        public event EventHandler OnHealthChanged;
        public event EventHandler OnHealthMaxChanged;
        public event EventHandler OnDamaged;
        public event EventHandler OnHealed;
        public event EventHandler OnDead;


        public float healthMax;
        public float armourMax;
        public float health;
        public float armour;

        public Health()
        {

        }
        public Health(float healthMax, float armourMax)
        {
            this.healthMax = healthMax;
            this.armourMax = armourMax;
            this.health = healthMax;
            this.armour = armourMax;
        }
        public void Initialize(float healthMax, float armourMax)
        {
            this.healthMax = healthMax;
            this.armourMax = armourMax;
            this.health = healthMax;
            this.armour = armourMax;

        }
        public float GetHealthArmourNormalized()
        {

            if (armourMax == 0)
            {
                Debug.LogError("GetHealthArmourNormalized() called before initialize!!!");
                armourMax = 1;
            }
            if (healthMax == 0) healthMax = 1;
            float retval;
            if (armour > 0)
            {
                retval = armour / armourMax;
            }
            else
            {
                retval = health / healthMax;
            }
            return retval;
        }
        public void Damage(float amount)
        {
            Debug.Log("Damage: " + amount);
            if (armour > 0)
            {
                armour -= amount;
                if (armour < 0) { armour = 0; }
            }
            else
            {
                health -= amount;
                if (health < 0) { health = 0; }
            }

            OnHealthChanged?.Invoke(this, EventArgs.Empty);
            OnDamaged?.Invoke(this, EventArgs.Empty);

            if (health <= 0)
            {
                Die();
            }
            Debug.Log("Current health:" + health + ", current armour: " + armour);
        }
        public void Die()
        {
            OnDead?.Invoke(this, EventArgs.Empty);
        }
        public bool IsDead()
        {
            return health <= 0;
        }

        public void Heal(float amount)
        {
            health += amount;
            if (health > healthMax)
            {
                health = healthMax;
            }
            OnHealthChanged?.Invoke(this, EventArgs.Empty);
            OnHealed?.Invoke(this, EventArgs.Empty);
        }
        public void HealComplete()
        {
            health = healthMax;
            OnHealthChanged?.Invoke(this, EventArgs.Empty);
            OnHealed?.Invoke(this, EventArgs.Empty);
        }
        public void SetHealthMax(float healthMax, bool fullHealth)
        {
            this.healthMax = healthMax;
            if (fullHealth) health = healthMax;
            OnHealthMaxChanged?.Invoke(this, EventArgs.Empty);
            OnHealthChanged?.Invoke(this, EventArgs.Empty);
        }
        public void SetHealth(float health)
        {
            if (health > healthMax)
            {
                health = healthMax;
            }
            if (health < 0)
            {
                health = 0;
            }
            this.health = health;
            OnHealthChanged?.Invoke(this, EventArgs.Empty);

            if (health <= 0)
            {
                Die();
            }
        }
        /// <summary>
        /// Tries to get a HealthSystem from the GameObject
        /// The GameObject can have either the built in HealthSystemComponent script or any other script that creates
        /// the HealthSystem and implements the IGetHealthSystem interface
        /// </summary>
        /// <param name="getHealthSystemGameObject">GameObject to get the HealthSystem from</param>
        /// <param name="healthSystem">output HealthSystem reference</param>
        /// <param name="logErrors">Trigger a Debug.LogError or not</param>
        /// <returns></returns>
        public static bool TryGetHealthSystem(GameObject getHealthSystemGameObject, out HealthSystemArmour healthSystem, bool logErrors = false)
        {
            healthSystem = null;

            if (getHealthSystemGameObject != null)
            {
                if (getHealthSystemGameObject.TryGetComponent(out IGetHealthSystemArmour getHealthSystemArmour))
                {
                    healthSystem = getHealthSystemArmour.GetHealthSystem();
                    if (healthSystem != null)
                    {
                        return true;
                    }
                    else
                    {
                        if (logErrors)
                        {
                            Debug.LogError($"Got HealthSystem from object but healthSystem is null! Should it have been created? Maybe you have an issue with the order of operations.");
                        }
                        return false;
                    }
                }
                else
                {
                    if (logErrors)
                    {
                        Debug.LogError($"Referenced Game Object '{getHealthSystemGameObject}' does not have a script that implements IGetHealthSystem!");
                    }
                    return false;
                }
            }
            else
            {
                // No reference assigned
                if (logErrors)
                {
                    Debug.LogError($"You need to assign the field 'getHealthSystemGameObject'!");
                }
                return false;
            }
        }

    }
}
