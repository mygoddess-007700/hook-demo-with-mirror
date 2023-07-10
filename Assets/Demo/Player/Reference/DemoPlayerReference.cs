using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class DemoPlayerReference : NetworkBehaviour
{
    public Transform rightHand;
    public Transform hookHand;
    public Transform leftHand;
    public Transform playerController;
    public Transform shootHook;
    public HookLeader hookLeader;
    public Hook hook;

    // private void Start()
    // {
    //     if (ClientPlayerReference.instance != null)
    //     {
    //         ClientPlayerReference.instance.localPlayerReferences.Add(connectionToServer, this);
    //     }
    // }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (ClientPlayerReference.instance != null)
        {
            ClientPlayerReference.instance.localPlayerReferences.Add(GetComponent<NetworkIdentity>(), this);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (ClientPlayerReference.instance != null)
            ClientPlayerReference.instance.localPlayerReferences.Remove(GetComponent<NetworkIdentity>());
    }
}
