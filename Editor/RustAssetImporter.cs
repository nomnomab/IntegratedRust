using System.Collections.Generic;
using System.IO;
using System.Linq;
using fts;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Nomnom.IntegratedRust.Editor {
  [ScriptedImporter(1, new [] { "rs", "toml" })]
  public class RustAssetImporter: ScriptedImporter {
    public override void OnImportAsset(AssetImportContext ctx) {
      string path = ctx.assetPath;
      string contents = File.ReadAllText(path);
      string extension = Path.GetExtension(path);
      
      TextAsset textAsset = new TextAsset(contents);
      extension = extension[1..];

      Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.nomnom.integrated-rust/Editor/Icons/{extension}.png");
      ctx.AddObjectToAsset("src", textAsset, tex);
      ctx.SetMainObject(textAsset);
    }
  }

  public class RustAssetPostProcessor : AssetPostprocessor {
    private static void OnPostprocessAllAssets(
      string[] importedAssets, 
      string[] deletedAssets, 
      string[] movedAssets,
      string[] movedFromAssetPaths) {

      IEnumerable<string> all = importedAssets
        .Concat(deletedAssets)
        .Concat(movedAssets)
        .Concat(movedFromAssetPaths);

      HashSet<string> cargoProjects = new HashSet<string>();
      bool hotReloadEnabled = EditorPrefs.GetBool(Cargo.KEY_HOT_RELOAD, false);

      if (!hotReloadEnabled && Application.isPlaying) {
        return;
      }

      foreach (string path in all) {
        if (string.IsNullOrEmpty(path)) {
          continue;
        }
        
        string extension = Path.GetExtension(path);
        string fullPath = Path.GetFullPath(path);
        
        switch (extension) {
          case ".rs":
            cargoProjects.Add(Cargo.GetCargoRoot(fullPath));
            break;
          case ".toml":
            cargoProjects.Add(Path.GetDirectoryName(fullPath));
            break;
        }

        if (cargoProjects.Count > 0 && Application.isPlaying && NativePluginLoader.singleton) {
          NativePluginLoader.singleton.Dispose();
        }

        foreach (string cargoProject in cargoProjects) {
          if (string.IsNullOrEmpty(cargoProject)) {
            continue;
          }
          
          DirectoryInfo dir = new DirectoryInfo(cargoProject);

          if (!dir.Exists) {
            continue;
          }
          
          // find asset
          foreach (FileInfo file in dir.EnumerateFiles()) {
            if (file.Extension == ".asset") {
              string assetPath = $"Assets\\{file.FullName[Application.dataPath.Length..]}".Replace("\\", "/");
              CargoAsset asset = AssetDatabase.LoadAssetAtPath<CargoAsset>(assetPath);

              if (!asset.AutoRecompile) {
                continue;
              }
              
              Cargo.Build(cargoProject, asset);
              break;
            }
          }
        }

        if (cargoProjects.Count > 0 && Application.isPlaying) {
          NativePluginLoader.Create();
          NativePluginLoader.singleton.Init(false);
        }
      }
    }
  }
}