using UnityEngine;

namespace Nomnom.IntegratedRust {
  [CreateAssetMenu]
  public class CargoAsset: ScriptableObject {
    public string Name;
    public bool AutoRecompile = true;
    [Tooltip("A path relative to Assets/")]
    public string BuildDir;
    public string DllOutputDir = "Plugins";
    public string CustomBuildArgs;
  }
}