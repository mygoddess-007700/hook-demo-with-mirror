using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ServerPlayerReference : NetworkBehaviour
{
    private static ServerPlayerReference _instance;

    public static ServerPlayerReference instance => _instance;

    private void Awake()
    {
        _instance = this;
    }
}