using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.HealthSystem
{
    public class HealthBarScroll : MonoBehaviour
    {
        private const float DAMAGED_HEALTH_SHRINK_TIMER_MAX = 1f;
        private Image barImage;
        private Image damagedBarImage;
        public Health healthSystemArmour;
        private Color damagedColor;
        private float damagedHealthShrinkTimer;
        private string gameObjectName;

        private void Awake()
        {
            barImage = transform.Find("bar").GetComponent<Image>();
            damagedBarImage = transform.Find("damagedBar").GetComponent<Image>();
            damagedColor = damagedBarImage.color;
            damagedColor.a = 0;
            if (healthSystemArmour == null) healthSystemArmour = new Health();

        }
        private void Update()
        {
            damagedHealthShrinkTimer -= Time.deltaTime;
            if (damagedHealthShrinkTimer < 0)
            {
                if (barImage.fillAmount < damagedBarImage.fillAmount)
                {
                    damagedBarImage.fillAmount -= Time.deltaTime * 2;
                }
            }
        }

        private void Start()
        {
            healthSystemArmour.OnDamaged += Health_OnDamaged;
            healthSystemArmour.OnHealed += Health_OnHealed;
            healthSystemArmour.OnDead += Health_OnDead;
            float fillAmount = 1;
            barImage.fillAmount = fillAmount;
            damagedBarImage.fillAmount = barImage.fillAmount;
        }

        private void Health_OnHealed(object sender, EventArgs e)
        {
            SetHealth(healthSystemArmour.GetHealthArmourNormalized());
            damagedBarImage.fillAmount = barImage.fillAmount;
        }
        private void Health_OnDamaged(object sender, EventArgs e)
        {
            //if (damagedColor.a <= 0)
            //{
            //    damagedBarImage.fillAmount = barImage.fillAmount;
            //}
            //damagedColor.a = 1;
            //damagedBarImage.color = damagedColor;
            //damagedHealthFadeTimer = DAMAGED_HEALTH_FADE_TIMER_MAX;
            damagedHealthShrinkTimer = DAMAGED_HEALTH_SHRINK_TIMER_MAX;

            float normalized = healthSystemArmour.GetHealthArmourNormalized();
            SetHealth(normalized);
        }

        private void SetHealth(float healthNormalized)
        {
            //healthSystemArmour.SetHealth(healthNormalized);
            barImage.fillAmount = healthNormalized;
        }
        private void Health_OnDead(object sender, EventArgs e)
        {
            Debug.Log("Dead!");
        }
    }
}
