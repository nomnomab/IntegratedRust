using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nomnom.IntegratedRust.Editor {
  [CustomEditor(typeof(CargoAsset))]
  public class CargoAssetEditor: UnityEditor.Editor {
    private bool? _wasBuilt;
    
    public override void OnInspectorGUI() {
      GUILayout.Space(8);

      EditorGUILayout.BeginHorizontal();
      {
        if (GUILayout.Button(new GUIContent("Build", "Runs a recompilation of this cargo project."))) {
          CargoAsset asset = (CargoAsset)target;
          string path = AssetDatabase.GetAssetPath(asset);
          
          path = Path.GetFullPath(path);
          path = Path.GetDirectoryName(path);
        
          bool isSuccess = Cargo.Build(path, asset);
          _wasBuilt = isSuccess;
        }

        if (_wasBuilt.HasValue) {
          Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.nomnom.integrated-rust/Editor/Icons/{(_wasBuilt.Value ? "check-mark" : "cancel")}.png");
          GUILayout.Box(tex, GUILayout.Width(16), GUILayout.Height(16));
        }
      }
      EditorGUILayout.EndHorizontal();
      
      GUILayout.Space(8);
      
      base.OnInspectorGUI();
    }
  }
}