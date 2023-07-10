using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ClientPlayerReference : MonoBehaviour
{
    public Dictionary<NetworkIdentity, DemoPlayerReference> localPlayerReferences =
        new Dictionary<NetworkIdentity, DemoPlayerReference>();

    private static ClientPlayerReference _instance;

    public static ClientPlayerReference instance => _instance;

    public ButtonController left;
    public ButtonController right;
    public ButtonController shoot;


    private void Start()
    {
        _instance = this;
        #if !UNITY_ANDROID || UNITY_EDITOR
        left.gameObject.SetActive(false);
        right.gameObject.SetActive(false);
        shoot.gameObject.SetActive(false);
        #endif
    }
}