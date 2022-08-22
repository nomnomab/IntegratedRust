using fts;
using UnityEngine;

namespace Nomnom.IntegratedRust.Samples.BasicRustPackage {
  public class Test: MonoBehaviour {
    [SerializeField] private Vector2Int _gridSize = new Vector2Int(100, 100);
    [SerializeField] private float _offset;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Material _material;
    [SerializeField] private float _amplitude = 1;
    [SerializeField] private float _frequency = 1;

    private Transform[] _transforms;
    
    private void Start() {
      _transforms = new Transform[_gridSize.x * _gridSize.y];
      
      for (int i = 0; i < _gridSize.x * _gridSize.y; i++) {
        (int x, int y) = (i % _gridSize.x, i / _gridSize.x % _gridSize.y);
        Vector3 position = new Vector3(x + _offset * x, 0, y + _offset * y);
        GameObject instance = Instantiate(_prefab, position, Quaternion.identity);
        
        instance.transform.SetParent(transform);
        instance.GetComponent<MeshRenderer>().sharedMaterial = _material;
        _transforms[i] = instance.transform;
      }
    }

    private void FixedUpdate() {
      if (!RsTestBindings.IsAvailable) {
        return;
      }
      
      float time = Time.time;
      _material.color = RsTestBindings.get_color();
    
      for (int i = 0; i < _transforms.Length; i++) {
        (int x, int y) = (i % _gridSize.x, i / _gridSize.x % _gridSize.y);
        Transform transform = _transforms[i];
        float timeOffset = time + x + y;
        Vector2 position = RsTestBindings.get_position(timeOffset, _amplitude, _frequency);
        
        transform.localPosition += new Vector3(position.x, position.y);
      }
    }

    private void OnDestroy() {
      NativePluginLoader.singleton.Dispose();
    }
  }
}