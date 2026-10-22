using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace YeogiCafe.EditorTools
{
    // [실험] URP 2D 조명 켜기/끄기. 켜면 프로젝트의 URP 에셋(2D 렌더러)을 그래픽·품질 설정에 연결하고,
    // 끄면 연결을 풀어 원래(기본 렌더링)로 돌아간다. 에셋은 지우지 않으니 언제든 다시 켤 수 있다.
    public static class LightingExperiment
    {
        [MenuItem("여기카페냐옹/조명 실험 켜기 (URP 2D)", priority = 300)]
        public static void On()
        {
            var asset = FindAsset();
            if (asset == null)
            {
                EditorUtility.DisplayDialog("조명 실험",
                    "URP 에셋이 아직 없어요.\nProject 창 Assets 폴더에서 오른쪽 클릭 → Create → Rendering → URP Asset (with 2D Renderer) 를 만든 뒤 다시 눌러 주세요.", "확인");
                return;
            }
            GraphicsSettings.defaultRenderPipeline = asset;
            int cur = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = asset; }
            QualitySettings.SetQualityLevel(cur, false);
            IncludeShader("Universal Render Pipeline/2D/Sprite-Unlit-Default");   // 바깥 풍경·조명 갓용(빌드에서 Shader.Find로 찾음)
            AssetDatabase.SaveAssets();
            Debug.Log("[조명 실험] 켬: " + AssetDatabase.GetAssetPath(asset));
        }

        [MenuItem("여기카페냐옹/조명 실험 끄기 (원래대로)", priority = 301)]
        public static void Off()
        {
            GraphicsSettings.defaultRenderPipeline = null;
            int cur = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = null; }
            QualitySettings.SetQualityLevel(cur, false);
            AssetDatabase.SaveAssets();
            Debug.Log("[조명 실험] 끔(기본 렌더링)");
        }

        static void IncludeShader(string name)
        {
            var sh = Shader.Find(name);
            if (sh == null) { Debug.LogWarning("[조명 실험] 셰이더 없음: " + name); return; }
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (gs == null || gs.Length == 0) return;
            var so = new SerializedObject(gs[0]);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            so.ApplyModifiedProperties();
        }

        static UniversalRenderPipelineAsset FindAsset()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) return a;
            }
            return null;
        }
    }
}
