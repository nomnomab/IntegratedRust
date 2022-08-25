using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using fts;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nomnom.IntegratedRust.Editor {
  public static class Cargo {
    public const string KEY_HOT_RELOAD = "Nomnom.Rust.HotReload";
    
    private static readonly Regex _warningRegex = new Regex("(?s)(warning+:.*?)(?:\r*\n){2}");
    private static readonly Regex _errorRegex = new Regex("(?s)(error.*?)(?:\r*\n){2}");
    
    [MenuItem("Assets/Create/Nomnom/Rust/Cargo Project")]
    private static void CreateCargoProject() {
      string path = GetPath();
      Debug.Assert(!string.IsNullOrEmpty(path));

      Create(path);
    }

    [MenuItem("Assets/Create/Nomnom/Rust/Cargo Project", true)]
    private static bool CreateCargoProjectValidation() {
      string path = GetPath();

      if (string.IsNullOrEmpty(path)) {
        return false;
      }

      DirectoryInfo info = new DirectoryInfo(path);
      int directoryLength = info.GetDirectories().Length;
      int fileLength = info.GetFiles().Length;
      
      if (directoryLength != 0 || fileLength != 0) {
        Debug.LogWarning($"Directories: {directoryLength}, Files: {fileLength}");
        return false;
      }

      return true;
    }

    [MenuItem("Tools/Nomnom/Rust/Hot reload")]
    private static void ToggleHotReload() {
      bool newValue = !EditorPrefs.GetBool(KEY_HOT_RELOAD, false);
      EditorPrefs.SetBool(KEY_HOT_RELOAD, newValue);
    }
    
    [MenuItem("Tools/Nomnom/Rust/Hot reload", true)]
    private static bool ToggleHotReloadValidate() {
      Menu.SetChecked("Tools/Nomnom/Rust/Hot reload", EditorPrefs.GetBool(KEY_HOT_RELOAD, false));
      return true;
    }

    [MenuItem("Tools/Nomnom/Rust/Build All")]
    private static void BuildAll() {
      IEnumerable<string> assets = AssetDatabase.FindAssets("t:CargoAsset").Select(AssetDatabase.GUIDToAssetPath);
      foreach (string asset in assets) {
        string dir = Path.GetDirectoryName(asset);
        Build(dir, AssetDatabase.LoadAssetAtPath<CargoAsset>(asset));
      }
    }
    
    private static string GetPath() {
      string path = Selection.assetGUIDs.FirstOrDefault();

      if (string.IsNullOrEmpty(path)) {
        Debug.LogWarning("A path was not provided");
        return null;
      }

      path = AssetDatabase.GUIDToAssetPath(path);

      if (!string.IsNullOrEmpty(Path.GetExtension(path))) {
        Debug.LogWarning("A file was selected");
        return null;
      }

      path = Path.GetFullPath(path);

      return path;
    }

    public static void Create(string dir) {
      string assetPath = $"Assets{dir[Application.dataPath.Length..]}".Replace("\\", "/");
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo {
        WindowStyle = ProcessWindowStyle.Normal,
        FileName = "cmd.exe",
        WorkingDirectory = dir,
        Arguments = "/C cargo init --lib"
      };
      
      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();

      string name = Path.GetFileNameWithoutExtension(dir);
      string tomlPath = $"{dir}\\Cargo.toml";
      string content = File.ReadAllText(tomlPath);
      content += "\n[lib]\ncrate-type = [\"cdylib\"]";
      File.WriteAllText(tomlPath, content);
      
      AssetDatabase.Refresh();
      
      CargoAsset cargoAsset = ScriptableObject.CreateInstance<CargoAsset>();
      cargoAsset.BuildDir = $@"..\Library\Rust\{name}";
      cargoAsset.Name = name;

      AssetDatabase.CreateAsset(cargoAsset, $"{assetPath}/project.asset");
    }

    public static bool Build(string dir, CargoAsset asset) {
      string buildDir = Path.Combine(Application.dataPath, asset.BuildDir);

      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo {
        WindowStyle = ProcessWindowStyle.Normal,
        FileName = "cmd.exe",
        WorkingDirectory = dir,
        Arguments = $"/C cargo build {asset.CustomBuildArgs} --target-dir \"{buildDir}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      string targetName = Path.GetFileName(dir);
      int progressId = Progress.Start($"Compiling {targetName}");
      Progress.Report(progressId, 0.5f, $"Compiling {targetName}");

      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();

      string error = process.StandardError.ReadToEnd();
      Debug.Log(error);
      
      MatchCollection warnings = _warningRegex.Matches(error);
      MatchCollection errors = _errorRegex.Matches(error);

      foreach (Match match in warnings) {
        Debug.LogWarning(match.Value);
      }
        
      foreach (Match match in errors) {
        Debug.LogError(match.Value);
      }

      bool isSuccess = errors.Count == 0;

      if (isSuccess) {
        // build dll
        string dllName = $"{targetName}.dll";
        Progress.Report(progressId, 1f, $"Building {dllName}");

        try {
          DirectoryInfo buildDirInfo = new DirectoryInfo(buildDir);
          string parsedName = asset.Name.Replace("-", "_");
          string dllStartDir = $"{buildDirInfo.GetDirectories()[0]}\\{parsedName}.dll";
          string dllEndDir = $"{Path.Combine(Application.dataPath, asset.DllOutputDir)}\\{asset.Name}.dll";
          string outputPath = Path.GetDirectoryName(dllEndDir);

          if (!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
          }

          File.Copy(dllStartDir, dllEndDir, true);
          AssetDatabase.Refresh();
          
          foreach (DirectoryInfo folder in buildDirInfo.EnumerateDirectories()) {
            folder.Delete(true);
          }
        } catch (Exception e) {
          Debug.LogException(e);
        }
      }
      
      Progress.Remove(progressId);

      return isSuccess;
    }

    public static string GetCargoRoot(string dir) {
      while (!string.IsNullOrEmpty(dir)) {
        DirectoryInfo parent = Directory.GetParent(dir);

        if (parent == null) {
          break;
        }
        
        dir = parent.FullName;

        bool foundToml = false;
        bool foundCargoAsset = false;

        if (!parent.Exists) {
          continue;
        }
        
        foreach (FileInfo file in parent.EnumerateFiles()) {
          switch (file.Extension) {
            case ".toml": foundToml = true;
              break;
            case ".asset": foundCargoAsset = true;
              break;
            default: continue;
          }
        }

        if (!(foundToml && foundCargoAsset)) {
          continue;
        }

        return dir;
      }

      return null;
    }
  }
}