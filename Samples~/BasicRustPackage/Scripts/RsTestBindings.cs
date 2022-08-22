using fts;
using UnityEngine;

namespace Nomnom.IntegratedRust.Samples.BasicRustPackage {
  [PluginAttr("rs-test")]
  public static class RsTestBindings {
    public static bool IsAvailable = false;
    
    [PluginFunctionAttr("get_position")]
    public static GetPosition get_position;
    public delegate Vector2 GetPosition(float time, float amplitude, float frequency);
    
    [PluginFunctionAttr("get_color")]
    public static GetColor get_color;
    public delegate Color32 GetColor();
  }
}