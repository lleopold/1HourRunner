//Description : footstepSystemEditor. Work in association with footstepSystem. Allow to manage player foostep sound depending the surface
#if (UNITY_EDITOR)
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;


namespace HP.Generics
{
	[CustomEditor(typeof(characterMovement))]
	public class characterMovementEditor : Editor
	{
		SerializedProperty SeeInspector;                                            // use to draw default Inspector

		public List<string> s_inputListJoystickAxis = new List<string>();
		public List<string> s_inputListJoystickButton = new List<string>();
		public List<string> s_inputListKeyboardAxis = new List<string>();
		public List<string> s_inputListKeyboardButton = new List<string>();

		public List<string> s_inputListJoystickBool = new List<string>();
		public List<string> s_inputListKeyboardBool = new List<string>();

		public GameObject objCanvasInput;


		private Texture2D MakeTex(int width, int height, Color col)
		{                       // use to change the GUIStyle
			Color[] pix = new Color[width * height];
			for (int i = 0; i < pix.Length; ++i)
			{
				pix[i] = col;
			}
			Texture2D result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
			return result;
		}

		private Texture2D Tex_01;                                                       // 
		private Texture2D Tex_02;
		private Texture2D Tex_03;
		private Texture2D Tex_04;
		private Texture2D Tex_05;

		public string selectedTag = "";

		public string newTagName = "";

		void OnEnable()
		{
			// Setup the SerializedProperties.
			SeeInspector = serializedObject.FindProperty("SeeInspector");

			Tex_01 = MakeTex(2, 2, new Color(1, .8f, 0.2F, .4f));
			Tex_02 = MakeTex(2, 2, new Color(1, .8f, 0.2F, .4f));
			Tex_03 = MakeTex(2, 2, new Color(.3F, .9f, 1, .5f));
			Tex_04 = MakeTex(2, 2, new Color(1, .3f, 1, .3f));
			Tex_05 = MakeTex(2, 2, new Color(1, .5f, 0.3F, .4f));

			GameObject tmp = GameObject.Find("InputsManager");
			if (tmp)
			{
				objCanvasInput = tmp;
			}
		}


		public override void OnInspectorGUI()
		{
			if (SeeInspector.boolValue)                         // If true Default Inspector is drawn on screen
				DrawDefaultInspector();

			serializedObject.Update();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("See Inspector :", GUILayout.Width(85));
			EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""), GUILayout.Width(30));
			EditorGUILayout.EndHorizontal();

			GUIStyle style_Yellow_01 = new GUIStyle(GUI.skin.box); style_Yellow_01.normal.background = Tex_01;
			GUIStyle style_Blue = new GUIStyle(GUI.skin.box); style_Blue.normal.background = Tex_03;
			GUIStyle style_Purple = new GUIStyle(GUI.skin.box); style_Purple.normal.background = Tex_04;
			GUIStyle style_Orange = new GUIStyle(GUI.skin.box); style_Orange.normal.background = Tex_05;
			GUIStyle style_Yellow_Strong = new GUIStyle(GUI.skin.box); style_Yellow_Strong.normal.background = Tex_02;

			//GUILayout.Label("");
			characterMovement myScript = (characterMovement)target;

			serializedObject.ApplyModifiedProperties();
		}

		void OnSceneGUI()
		{
		}
	}
}

#endif