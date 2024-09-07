using System;
using System.Collections.Generic;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

public class NetworkObject : MonoBehaviour, ICreateGameObjectHierarchy, IChangeComponentProperties, IChangeGameObjectProperties, IChangeGameObjectStructure, IChangeGameObjectStructureHierarchy
{
    [SerializeField] private string serializeFieldSceneGuid;
    private string _resetBufferSceneGuid;

    [SerializeField] private string prefabPath;

    public static Dictionary<string, NetworkObject> NetworkObjects { get; } = new();

    public string SceneGuid => serializeFieldSceneGuid;
    public string PrefabGuid => prefabPath;

    private void Reset()
    {
        ApplyResetBuffer();
    }
    
    private void Awake()
    {
        SetupSceneGuid();
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        ApplyScriptReloadBuffer();
        SetupAll();
    }
    
    public void OnCreateGameObjectHierarchy()
    {
        if (Application.isPlaying) return;
        
        SetupAll();
    }
    
    public void OnChangeGameObjectStructure()
    {
        if (Application.isPlaying) return;
        
        SetupAll();
    }
    
    public void OnChangeComponentProperties()
    {
        if (Application.isPlaying) return;
        
        SetupAll();
    }

    public void OnChangeGameObjectProperties()
    {
        if (Application.isPlaying) return;
        
        SetupAll();
    }
    
    public void OnChangeGameObjectStructureHierarchy()
    {
        if (Application.isPlaying) return;
        
        SetupAll();
    }
    
    /// <summary>
    /// If a Component get's resetted, all Serialize Field values are lost. This method will reapply the lost values
    /// for the Serialize Fields with the Reset Buffer. This prevents loosing the original guid.
    /// </summary>
    private void ApplyResetBuffer()
    {
        serializeFieldSceneGuid = _resetBufferSceneGuid;
    }

    /// <summary>
    /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
    /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
    /// prevents loosing the original guid.
    /// </summary>
    private void ApplyScriptReloadBuffer()
    {
        _resetBufferSceneGuid = serializeFieldSceneGuid;
    }

    private void SetupAll()
    {
        if (gameObject.scene.name != null)
        {
            SetupSceneGuid();
        }
        else
        {
            ResetSceneGuid();
        }
        
        SetDirty(this);
    }
    
    private void SetupSceneGuid()
    {
        if (!string.IsNullOrEmpty(serializeFieldSceneGuid))
        {
            if (NetworkObjects.TryGetValue(serializeFieldSceneGuid, out NetworkObject networkObject))
            {
                if (networkObject != this)
                {
                    SetSceneGuidGroup(Guid.NewGuid().ToString());
                    NetworkObjects.Add(serializeFieldSceneGuid, this);
                }
            }
            else
            {
                NetworkObjects.Add(serializeFieldSceneGuid, this);
            }
        }
        else
        {
            SetSceneGuidGroup(Guid.NewGuid().ToString());
        }
    }

    private void ResetSceneGuid()
    {
        SetSceneGuidGroup("");
    }

    public void SetSceneGuidGroup(string guid)
    {
        serializeFieldSceneGuid = guid;
        _resetBufferSceneGuid = guid;
    }
    
    public void SetPrefabPath(string newPrefabPath)
    {
        prefabPath = newPrefabPath;
    }
    
    private void SetDirty(UnityEngine.Object obj)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(obj);
        }
#endif
    }
}
