using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Hook : NetworkBehaviour
{
    private Transform mTransform; //reference to this transform
    private Transform hookModelTransdform; //reference to the hook model

    public HookLeader hookLeader;
    public List<GameObject> hookObjects; //enemies got hooked
    public float targetBondDamping = 10.0f; //damping for bond target
    public float targetBondDistance = 0.2f; //the distance for bond target object
    public float hookRotateSpeed = 500; //hook rotating speed
    public HookEventListener hookEventListener; //hook take back listener       
    public Transform ownerTrans; //hook owner

    public List<NetworkIdentity> hookConnectionPlayers;

    private DemoPlayerReference _demoPlayerReference;
    private bool isEndInit = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.isOwned)
        {
            return;
        }
    
        InitHook();
    }

    private void InitHook()
    {
        hookObjects = new List<GameObject>();
        hookConnectionPlayers = new List<NetworkIdentity>();
        gameObject.AddComponent<HookEventListener>();
        mTransform = transform;
        hookModelTransdform = mTransform.Find("HookModel");

        ClientPlayerReference.instance.localPlayerReferences.TryGetValue(NetworkClient.connection.identity, out _demoPlayerReference);
        _demoPlayerReference.hook = this;

        setOwnerTrans(_demoPlayerReference.hookHand);
        hookLeader = _demoPlayerReference.hookLeader;
        hookEventListener.StartTakeBackHook += hookLeader.StartTakeBackHook;
        hookEventListener.HookSomething += hookLeader.HookSomething;
        

        Physics.IgnoreCollision(GetComponent<Collider>(),
            _demoPlayerReference.rightHand.gameObject.GetComponent<Collider>());
        Physics.IgnoreCollision(GetComponent<Collider>(),
            _demoPlayerReference.leftHand.gameObject.GetComponent<Collider>());
        Physics.IgnoreCollision(GetComponent<Collider>(),
            _demoPlayerReference.playerController.gameObject.GetComponent<Collider>());

        _demoPlayerReference.hookLeader.hookSpawned = true;
        isEndInit = true;
    }

    public void setOwnerTrans(Transform owner)
    {
        ownerTrans = owner;
    }

    private void FixedUpdate()
    {
        if (!base.isOwned || !isEndInit)
        {
            return;
        }
        
        hookModelTransdform.Rotate(Vector3.up * Time.deltaTime * hookRotateSpeed, Space.Self);
        if (hookObjects.Count > 0 && mTransform != null)
        {
            foreach (GameObject obj in hookObjects)
            {
                FollowHook(mTransform, obj.transform);
            }
        }
    
        if (hookConnectionPlayers.Count > 0 && mTransform != null)
        {
            foreach (NetworkIdentity identity in hookConnectionPlayers)
            {
                PlayerFollowHook(identity, mTransform);
            }
        }
    }

    public void DespawnHook()
    {
        if (hookObjects.Count > 0)
        {
            if (ownerTrans != null)
            {
                foreach (GameObject obj in hookObjects)
                {
                    Physics.IgnoreCollision(ownerTrans.GetComponent<Collider>(),
                        obj.transform.GetComponent<Collider>(), false);
                    obj.GetComponent<Rigidbody>().useGravity = true;
                }

                foreach (NetworkIdentity identity in hookConnectionPlayers)
                {
                    RecoveryPlayerCollision(identity);
                }
            }

            hookObjects.Clear();
            hookConnectionPlayers.Clear();
        }

        RpcServerDespawnHook();           
    }

    [Command]
    private void RpcServerDespawnHook(NetworkConnectionToClient conn = null)
    {
        NetworkServer.Destroy(gameObject);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!base.isOwned || !isEndInit)
        {
            return;
        }

        if (collider.gameObject.tag == "enemy")
        {
            //enemy got hooked 
            if (!hookObjects.Contains(collider.gameObject))
                hookObjects.Add(collider.gameObject);
            if (ownerTrans != null)
            {
                Physics.IgnoreCollision(ownerTrans.GetComponent<Collider>(), collider, true);
                collider.GetComponent<Rigidbody>().useGravity = false;
            }

            hookEventListener.NotifyTakeBack();
        }
        else if (collider.gameObject.tag == "Player")
        {
            //player got hooked
            if (collider.gameObject.GetComponent<NetworkIdentity>())
            {
                NetworkIdentity identity = collider.gameObject.GetComponent<NetworkIdentity>();
                if (identity == hookLeader._demoPlayerReference.GetComponent<NetworkIdentity>())
                {
                    return;
                }

                if (!hookConnectionPlayers.Contains(identity))
                    hookConnectionPlayers.Add(identity);
                if (ownerTrans != null)
                {
                    IgnorePlayerCollision(identity);
                }
                
                hookEventListener.NotifyTakeBack();
            }
            else
            {
                Debug.Log("player got hooked but NetworkBehavivor is null");
            }
        }
        else if (collider.gameObject.tag == "cylinder")
        {
            hookEventListener.NotifyHookSomething(mTransform.position);
        }
    }

    public void IgnorePlayerCollision(NetworkIdentity identity)
    {
        Rigidbody ownerHand = _demoPlayerReference.hookHand.GetComponent<Rigidbody>();

        DemoPlayerReference reference;
        ClientPlayerReference.instance.localPlayerReferences.TryGetValue(identity, out reference);
        Rigidbody playerController = reference.playerController.GetComponent<Rigidbody>();
        Rigidbody leftHand = reference.leftHand.GetComponent<Rigidbody>();
        Rigidbody rightHand = reference.rightHand.GetComponent<Rigidbody>();

        Physics.IgnoreCollision(ownerHand.GetComponent<Collider>(), playerController.GetComponent<Collider>(),
            true);
        Physics.IgnoreCollision(ownerHand.GetComponent<Collider>(), leftHand.GetComponent<Collider>(), true);
        Physics.IgnoreCollision(ownerHand.GetComponent<Collider>(), rightHand.GetComponent<Collider>(), true);
    }

    public void RecoveryPlayerCollision(NetworkIdentity identity)
    {
        Rigidbody ownerHand = transform.parent.GetComponent<Rigidbody>();

        DemoPlayerReference reference;
        ClientPlayerReference.instance.localPlayerReferences.TryGetValue(identity, out reference);
        Rigidbody playerController = reference.playerController.GetComponent<Rigidbody>();
        Rigidbody leftHand = reference.leftHand.GetComponent<Rigidbody>();
        Rigidbody rightHand = reference.rightHand.GetComponent<Rigidbody>();

        Physics.IgnoreCollision(ownerHand.GetComponent<Collider>(), playerController.GetComponent<Collider>(),
            false);
        Physics.IgnoreCollision(ownerHand.GetComponent<Collider>(), leftHand.GetComponent<Collider>(), false);
        Physics.IgnoreCollision(ownerHand.GetComponent<Collider>(), rightHand.GetComponent<Collider>(), false);
    }

    void FollowHook(Transform prevNode, Transform follower)
    {
        //make follower follow the node
        Quaternion targetRotation = Quaternion.LookRotation(prevNode.position - follower.position, prevNode.up);
        targetRotation.x = 0f;
        targetRotation.z = 0f;
        follower.rotation = Quaternion.Slerp(follower.rotation, targetRotation, Time.deltaTime * targetBondDamping);

        Vector3 targetPosition = prevNode.position;
        targetPosition -= follower.rotation * Vector3.forward * targetBondDistance;
        targetPosition.y = follower.position.y;
        follower.position = Vector3.Lerp(follower.position, targetPosition, Time.deltaTime * targetBondDamping);
    }

    private void PlayerFollowHook(NetworkIdentity identity, Transform _hookTransform)
    {
        RpcServerPlayerFollowHook(identity, _hookTransform.position, targetBondDamping);
    }

    [Command]
    public void RpcServerPlayerFollowHook(NetworkIdentity identity, Vector3 _position, float damp, NetworkConnectionToClient conn = null)
    {
        Debug.Log("MoveCommand");
        RpcTargetFollow(identity.connectionToClient, _position, damp);
    }
    
    [TargetRpc]
    public void RpcTargetFollow(NetworkConnectionToClient target, Vector3 _position, float damp)
    {
        Debug.Log("MoveTarget");
        DemoPlayerReference reference;
        ClientPlayerReference.instance.localPlayerReferences.TryGetValue(NetworkClient.connection.identity,
            out reference);
        _position.y = reference.playerController.position.y;
        
        Quaternion targetRotation = Quaternion.LookRotation(_position - reference.playerController.position,
                transform.up);
        targetRotation.x = 0f;
        targetRotation.z = 0f;
        
        Vector3 targetPosition = _position;
        
        var playerController = reference.playerController.GetComponent<Rigidbody>();
        playerController.position = targetPosition;
        playerController.rotation = targetRotation;
    }
}