using System;
using Game.Editor.Common;
using Game.Gameplay.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Editor.UiAssets
{
    /// <summary>
    /// Ассеты интерфейса и настроек: PanelSettings (с темой GameTheme.tss) и ControlSettingsConfig.
    /// Стили (.uss) и тема (.tss) - текстовые файлы в _Game/Art/UI, их правят напрямую.
    /// Batchmode: -executeMethod Game.Editor.UiAssets.UiAssetsBuilder.BuildFromCommandLine
    /// </summary>
    public static class UiAssetsBuilder
    {
        public const string ThemePath = "Assets/_Game/Art/UI/GameTheme.tss";
        public const string HudStylePath = "Assets/_Game/Art/UI/Hud.uss";
        public const string SettingsMenuStylePath = "Assets/_Game/Art/UI/SettingsMenu.uss";
        public const string PanelSettingsPath = "Assets/_Game/Data/UI/GamePanelSettings.asset";
        public const string ControlSettingsConfigPath = "Assets/_Game/Data/Settings/ControlSettingsConfig.asset";

        private static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);
        private const float WidthHeightMatch = 0.5f;

        [MenuItem("Tools/Gostgame/Build UI Assets")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void Build()
        {
            EditorAssetUtility.LoadOrCreateScriptableObject<ControlSettingsConfig>(ControlSettingsConfigPath);

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                throw new InvalidOperationException($"Не найдена тема UI {ThemePath}");
            }

            var panel = EditorAssetUtility.LoadOrCreateScriptableObject<PanelSettings>(PanelSettingsPath);
            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = ReferenceResolution;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = WidthHeightMatch;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();

            Debug.Log($"[UiAssetsBuilder] Готово: {PanelSettingsPath}, {ControlSettingsConfigPath}");
        }
    }
}
