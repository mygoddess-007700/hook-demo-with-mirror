using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    private static MainCamera _instance;

    private Camera _camera;
    
    public MainCameraTracker AttachedTo
    {
        get;
        private set;
    }

    public static MainCamera Instance
    {
        get
        {
            if (!_instance)
                _instance = FindObjectOfType<MainCamera>();

            return _instance;
        }
    }

    public Camera Camera
    {
        get
        {
            if (!_camera)
                _camera = GetComponent<Camera>();

            return _camera;
        }
    }
    
    protected void Awake()
    {
        if (!_instance)
            _instance = this;
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    private void LateUpdate()
    {
        var instance = Instance;
        if (!instance || !instance.AttachedTo) return;

        var transform = instance.transform;
        var attachedToTransform = instance.AttachedTo.transform;
        var pos = attachedToTransform.position;
        pos.y += 2f;
        pos -= attachedToTransform.forward * 5;
        transform.position = pos;
        transform.rotation = attachedToTransform.rotation;
    }
    
    public void Attach(MainCameraTracker tracker)
    {
        if (AttachedTo)
            AttachedTo.AttachedCamera = null;

        AttachedTo = tracker;
        
    }
}
