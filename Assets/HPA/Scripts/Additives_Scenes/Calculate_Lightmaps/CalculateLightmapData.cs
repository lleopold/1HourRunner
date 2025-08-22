using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics
{
    [CreateAssetMenu(fileName = "CalculateLightmapData", menuName = "HP/CalculateLightmapData")]
    public class CalculateLightmapData : ScriptableObject
    {
        public bool MoreOptions;
        public bool HelpBox;

        public int tab = 0;

        public string projetTabScenePath = "Assets/HPA/Demo/";

        public string sceneThatContainsLight = "Gameplay_Scene";
        public string lightName = "SUN_Directional_Light";

        public bool sceneAutoSave = true;


        public List<string> sceneToCalculateList = new List<string>();
        public List<string> otherSceneToOpenList = new List<string>();
    }
}

