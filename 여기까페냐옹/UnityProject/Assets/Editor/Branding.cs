using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace YeogiCafe.EditorTools
{
    // 11/02: 실행 파일 아이콘·시작 화면(스플래시)을 Unity 기본에서 우리 것으로.
    //  - 아이콘: 스팀 바로가기 아이콘(검은 고양이) → exe·작업 표시줄·휴대폰 앱 아이콘
    //  - 스플래시: Wintery Studio 로고(눈 위를 걷는 흰 고양이) + 아래 작게 'Made with Unity'
    //    Unity Personal(2022.3)은 Unity 로고를 끌 수 없어서, 우리 로고 밑에 작게 붙는 방식(UnityLogoBelow)으로 둔다.
    //  빌드 메뉴(BuildTool)가 빌드 직전에 Apply()를 부른다. 메뉴에서 따로 눌러도 된다.
    public static class Branding
    {
        const string Dir = "Assets/Art/Branding/";
        const string Icon = Dir + "app_icon_512.png", Logo = Dir + "splash_logo.png";
        static readonly Color SplashBg = new Color(0.09f, 0.10f, 0.16f, 1f);   // 로고의 밤하늘색(어두운 남색)

        [MenuItem("여기카페냐옹/브랜딩 적용 (아이콘·시작 화면)", priority = 220)]
        public static void ApplyMenu() { Apply(); EditorUtility.DisplayDialog("브랜딩", "아이콘과 시작 화면을 적용했어요.\n(다음 빌드부터 반영)", "확인"); }

        public static void Apply()
        {
            Import(Icon, TextureImporterType.Default, false);
            Import(Logo, TextureImporterType.Sprite, true);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Icon);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(Logo);
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);           // 모든 플랫폼 기본 아이콘
                var std = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
                var arr = new Texture2D[std.Length]; for (int i = 0; i < arr.Length; i++) arr[i] = icon;
                PlayerSettings.SetIcons(NamedBuildTarget.Standalone, arr, IconKind.Any);
            }
            else Debug.LogWarning("[Branding] 아이콘 없음: " + Icon);

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.backgroundColor = SplashBg;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            if (logo != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
            else Debug.LogWarning("[Branding] 로고 없음: " + Logo);
            try { PlayerSettings.SplashScreen.showUnityLogo = false; } catch { }   // Pro면 꺼지고, Personal이면 무시됨(UnityLogoBelow로 작게)
            AssetDatabase.SaveAssets();
            Debug.Log("[Branding] 적용 완료");
        }

        static void Import(string path, TextureImporterType type, bool point)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { AssetDatabase.ImportAsset(path); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (ti == null) return;
            bool dirty = false;
            if (ti.textureType != type) { ti.textureType = type; dirty = true; }
            if (type == TextureImporterType.Sprite && ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            var fm = point ? FilterMode.Point : FilterMode.Bilinear;
            if (ti.filterMode != fm) { ti.filterMode = fm; dirty = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; dirty = true; }
            if (dirty) ti.SaveAndReimport();
        }
    }
}
