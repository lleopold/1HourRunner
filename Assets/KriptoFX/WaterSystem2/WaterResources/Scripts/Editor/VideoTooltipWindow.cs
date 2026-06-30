#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace KWS
{
    internal class VideoTooltipWindow : EditorWindow
    {
        public KWS_EditorVideoTipsSettings.VideoSettings TipSettings;

        GameObject  tempGO;
        VideoClip   clip;
        VideoPlayer player;
        Texture     currentRT;
        Texture2D   DefaultTexture;

        string UrlPath = @"https://kripto289.com/AssetStore/KWS2/VideoHelpers/";


        void OnGUI()
        {
            if (clip == null)
            {
                if (player == null)
                {
                    tempGO           = new GameObject("WaterVideoWindowHelp");
                    tempGO.hideFlags = HideFlags.DontSave;

                    player = tempGO.AddComponent<VideoPlayer>();

                    var url = Path.Combine(UrlPath, TipSettings.VideoClipFileName + ".mp4");

                    player.url       = url;
                    player.isLooping = true;

                    player.prepareCompleted += PlayerOnprepareCompleted;
                    player.Prepare();
                }
            }

            if (currentRT == null)
            {
                if (DefaultTexture == null) DefaultTexture = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_DefaultVideoLoading);
            }

            EditorGUI.DrawPreviewTexture(new Rect(0, 0, position.width, position.height), currentRT == null ? DefaultTexture : currentRT);

            var playerTime = (float)player.time;
            
            if (TipSettings.Descriptions.Length == 1)
            {
                DrawSubtitle(TipSettings.Descriptions[0].TitleText, TipSettings.Descriptions[0].DescriptionText);
                DrawProgressBar(TipSettings.Descriptions[0].StartTime, (float)player.length, playerTime);
            }
            else
            {
                foreach (var description in TipSettings.Descriptions)
                {
                    if (playerTime >= description.StartTime && playerTime < description.EndTime)
                    {
                        DrawSubtitle(description.TitleText, description.DescriptionText);
                        DrawProgressBar(description.StartTime, description.EndTime, playerTime);
                        break;
                    }
                }
            }
        }

        GUIStyle _subtitleStyle;

        void DrawSubtitle(string titleText, string descriptionText)
        {
            if (_subtitleStyle == null)
                _subtitleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize  = 20,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap  = true,
                    richText  = true,
                    normal    = { textColor = Color.white },
                    hover     = { textColor = Color.white }
                };

            string text    = $"<b>{titleText}:</b> {descriptionText}";
            var    content = new GUIContent(text);
            
            int paddingLeft   = 10;
            int paddingRight  = 10;
            int paddingTop    = 10;
            int paddingBottom = 5;

            var maxWidth   = position.width - (paddingLeft + paddingRight);
            var textHeight = _subtitleStyle.CalcHeight(content, maxWidth);
          
            var bgRect = new Rect(0, 0, position.width, textHeight + paddingTop + paddingBottom);
            EditorGUI.DrawRect(bgRect, new Color(0.15f, 0.15f, 0.15f, 0.85f));
          
            var textRect = new Rect(paddingLeft, paddingTop, maxWidth, textHeight);
            GUI.Label(textRect, content, _subtitleStyle);
        }


        void DrawProgressBar(float startTime, float endTime, float currentTime)
        {
            var progressRect = new Rect(0, 0, position.width, 5);
            EditorGUI.DrawRect(progressRect, new Color(0.3f, 0.3f, 0.3f, 0.8f));

            float t          = Mathf.Clamp01((float)((currentTime - startTime) / (endTime - startTime)));
            var   filledRect = new Rect(0, 0, position.width * t, 5);
            EditorGUI.DrawRect(filledRect, new Color(0.7f, 0.9f, 1f, 0.95f));


            float size = 40f;
            var buttonRect = new Rect(
                position.width  - size - 10,
                position.height - size - 10,
                size,
                size
            );

          
            EditorGUI.DrawRect(buttonRect, new Color(0f, 0f, 0f, 0.4f));

            var style = new GUIStyle(GUI.skin.button)
            {
                normal    = { background = Texture2D.blackTexture, textColor = Color.white },
                hover     = { background = Texture2D.blackTexture, textColor = Color.cyan },
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 20,
                fontStyle = FontStyle.Bold
            };

            string label = player.isPlaying ? "II" : "▶";

            if (GUI.Button(buttonRect, label, style))
            {
                if (player.isPlaying) player.Pause();
                else player.Play();
            }
        }


        private void PlayerOnprepareCompleted(VideoPlayer source)
        {
            player.Play();
            currentRT = source.texture;
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            if (player != null)
            {
                player.Stop();
                player.targetTexture = null;
            }

            KW_Extensions.SafeDestroy(tempGO);
            if (DefaultTexture != null) Resources.UnloadAsset(DefaultTexture);
        }

        void OnEditorUpdate()
        {
            if (player != null && (player.isPrepared || player.isPlaying))
                Repaint();
        }
    }
}

#endif