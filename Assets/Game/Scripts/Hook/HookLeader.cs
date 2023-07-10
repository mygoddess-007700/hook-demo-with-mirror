using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HookLeader : NetworkBehaviour
{
    private enum HookStatus
    {
        shooting,
        takeback,
        reverseInit,
        reverse
    }
    
    public DemoPlayerReference _demoPlayerReference;
    public int MaxNode = 35; //max number of nodes that can be in the chain
    public List<GameObject> nodes = new List<GameObject>(); //   hook nodes
    public Transform mTransform;
    public float bondDistance = 0.13f; //distance between nodes
    public float bondDamping = 100.0f; //the damping for bonding nodes
    public GameObject hookNodePrefab; //node prefab
    public GameObject hookPrefab; //hook prefab
    public float extendInterval = 0.05f; //bond node intervals
    public float takeBackInterval = 0f; //interval take back hook node
    public int shootHookSpeed = 1; //bond hook node number everytimes
    public int takeBackHookSpeed = 2; //take back hook node number every time
    public int minUpdateOrderNum = 5; //update node order when node count > THIS Parameter
    public int updateOrderSpeed = 2; //update hook node number everytime
    
    public bool lastHookNodeSpawned = true;
    public bool hookSpawned = false;
    
    private HookStatus hookStatus = HookStatus.shooting; //the status of hook is shooting when it was creating
    private GameObject hookNodeClone;
    private Transform hookStartTransform; // transform of the hook start
    private float extendTime; //time last node bond
    private float takeBackTime; //time last node take back
    private int nodeCount; //current count of hook node
    private float updateOrderTime; //time last update node order
    private LineRenderer lineRenderer; //hook chain's lineRender
    private bool shouldKeepPosition; //whether or not keep hook leader position
    private Vector3 keepPosition; //the position that hook should keep

    private bool isDestroy = false;
    private NetworkConnection _conn;
    private Hook hookScript;
    private bool isEndInit = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isOwned)
        {
            return;
        }

        InitHookLeader();
    }

    private void InitHookLeader()
    {
        shouldKeepPosition = false;
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer)
            lineRenderer.enabled = false;

        ClientPlayerReference.instance.localPlayerReferences.TryGetValue(NetworkClient.connection.identity, out _demoPlayerReference);
        
        _demoPlayerReference.hookLeader = this;

        if (updateOrderSpeed > minUpdateOrderNum)
        {
            updateOrderSpeed = minUpdateOrderNum;
        }

        mTransform = transform;

        hookStartTransform = _demoPlayerReference.shootHook; //get shoot point transform

        Vector3 position = nextPosition(mTransform);
        Quaternion rotation = nextRotation(mTransform, position);  
        SpawnHook(position, rotation);

        isEndInit = true;
    }

    private void FixedUpdate()
    {
        if (!isOwned || !isEndInit || isDestroy || !hookSpawned)
        {
            return;
        }
    
        FollowPlayer();
        HookLogic();
    }

    void HookLogic()
    {
        if (!lastHookNodeSpawned)
        {
            return;
        }
        
        if (!shouldKeepPosition)
        {
            if (hookStartTransform == null)
            {
                Debug.Log("is null");
            }

            //keep hookLeader follow owner's position, this will be activated when player hook a cylinder
            mTransform.position = hookStartTransform.position;
        }

        if (hookStatus == HookStatus.shooting)
        {
            if (nodeCount < MaxNode)
            {
                if (extendInterval > 0)
                {
                    if (Time.time - extendTime > extendInterval)
                    {
                        extendTime = Time.time;
                        AddHookNode(shootHookSpeed);
                        nodeCount += shootHookSpeed;
                    }
                }
                else
                {
                    AddHookNode(shootHookSpeed);
                    nodeCount += shootHookSpeed;
                    //hook node extending
                }
            }
            else
            {
                hookStatus = HookStatus.takeback;
            }
        }

        // pull back hook owner
        if (hookStatus == HookStatus.reverseInit)
        {
            //hook is now initating reversing
            shouldKeepPosition = true;
            mTransform.position = keepPosition; //set leader position to hook contact point

            nodes.Reverse(); //reverse the hook

            hookStatus = HookStatus.reverse; //start reverse
        }

        if (hookStatus == HookStatus.takeback || hookStatus == HookStatus.reverse)
        {
            //reverse has started, the hook is taking back
            if (nodes.Count > 0)
            {
                int speed = takeBackHookSpeed;
                if (hookStatus == HookStatus.takeback)
                {
                    if (takeBackInterval > 0)
                    {
                        if (Time.time - takeBackTime > takeBackInterval)
                        {
                            takeBackTime = Time.time;
                            TakeBackHook(speed);
                        }
                    }
                    else
                    {
                        TakeBackHook(speed);
                    }
                }


                if (hookStatus == HookStatus.reverse && _demoPlayerReference.hook != null)
                {
                    //when draging player, first disable the hookLeader and the hook's model.
                    //pull back hook owner
                    TakeBackReverseHook(10);
                    gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
                    _demoPlayerReference.hook.GetComponentInChildren<MeshRenderer>().enabled = false;
                    foreach (GameObject gameObject in nodes)
                    {
                        gameObject.GetComponent<Renderer>().enabled = false;
                    }
                }
            }

            if (nodes.Count <= 0)
            {
                if (hookStatus == HookStatus.reverse)
                {
                    PlayerMove(keepPosition);
                }

                _demoPlayerReference.hook.DespawnHook();
                RpcServerDespawnHookLeader();
                _demoPlayerReference.shootHook.GetComponent<ShootHook>().shootEnd = true;
                isDestroy = true;
            }
        }
    }
    
    private void FollowPlayer()
    {
        if (!lastHookNodeSpawned)
        {
            return;
        }
        
        // update hook nodes transform
        for (int i = 0; i < nodes.Count; i++)
        {
            FollowPrev(i == 0 ? mTransform : nodes[i - 1].transform, nodes[i].transform);
        }

        // update hook transform 
        if (_demoPlayerReference.hook != null)
        {
            //Debug.Log("update hook transform");
            HookFollowLast(LastNode(), _demoPlayerReference.hook.transform);
        }

        // Renderer hook path
        if (lineRenderer && nodes.Count >= 5)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
            {
                lineRenderer.SetPosition(i, nodes[i].transform.position);
            }
        }
    }

    [Command]
    private void RpcServerDespawnHookLeader(NetworkConnectionToClient conn = null)
    {
        NetworkServer.Destroy(gameObject);
    }
    
    public void StartTakeBackHook()
    {
        hookStatus = HookStatus.takeback;
    }

    public void HookSomething(Vector3 hookContactPoint)
    {
        if (nodeCount >= minUpdateOrderNum)
        {
            keepPosition = hookContactPoint;
            hookStatus = HookStatus.reverseInit;
        }
    }

    private void PlayerMove(Vector3 _position)
    {
        var playerController = _demoPlayerReference.playerController.GetComponent<Rigidbody>();
        var leftHand = _demoPlayerReference.leftHand.GetComponent<Rigidbody>();
        var rightHand = _demoPlayerReference.rightHand.GetComponent<Rigidbody>();

        Vector3 lastLeftHandLocalPos = playerController.position - leftHand.position;
        Vector3 lastRightHandLocalPos = playerController.position - rightHand.position;

        playerController.position = _position;
        leftHand.position = _position - lastLeftHandLocalPos;
        rightHand.position = _position - lastRightHandLocalPos;
    }

    void AddHookNode(int speed)
    {
        for (int i = 0; i < speed; i++)
        {
            Transform preTransform = LastNode();
            if (preTransform.GetComponent<HookNode>() != null)
            {
                if (preTransform.GetComponent<HookNode>().transRotation != Quaternion.identity)
                    preTransform.rotation = preTransform.GetComponent<HookNode>().transRotation;
            }
            
            Vector3 position = nextPosition(preTransform);
            Quaternion rotation = nextRotation(preTransform, position);               
            if (_demoPlayerReference.hookLeader.nodes.Count < _demoPlayerReference.hookLeader.MaxNode)
            {
                lastHookNodeSpawned = false;
                // SpawnHookNode(position, rotation, _demoPlayerReference.GetComponent<NetworkIdentity>());
                CmdSpawnHookNode(position, rotation);
            }
        }
    }

    void TakeBackHook(int speed)
    {
        //nodes are removing by order when the hook is reversing
        //remove node for take back hook chain
        for (int i = 0; i < speed; i++)
        {
            if (nodes.Count > 0)
            {
                HookNode node = nodes[0].GetComponent<HookNode>();
                node.RemoveMe();
                nodes.RemoveAt(0);
                if (nodes.Count == 0)
                {
                    break;
                }
            }
        }
    }
    
    void TakeBackReverseHook(int speed)
    {
        //nodes are removing by order when the hook is reversing
        //remove node for take back hook chain
        for (int i = 0; i < speed; i++)
        {
            if (nodes.Count > 0)
            {
                HookNode node = nodes[nodes.Count - 1].GetComponent<HookNode>();
                node.RemoveMe();
                nodes.RemoveAt(nodes.Count - 1);
                if (nodes.Count == 0)
                {
                    break;
                }
            }
        }
    }

    public Transform LastNode()
    {
        if (nodes.Count > 0)
        {
            return nodes[nodes.Count - 1].transform;
        }
        else
        {
            //Debug.Log("no node was created");
            return mTransform;
        }
    }

    private Vector3 nextPosition(Transform prevNode)
    {           
        Quaternion currentYRotation = Quaternion.Euler(0, prevNode.eulerAngles.y, 0);//alpha
        Vector3 posF = Vector3.forward * bondDistance * Mathf.Abs(Mathf.Cos(prevNode.eulerAngles.y));


        Quaternion currentXRotation = Quaternion.Euler(prevNode.eulerAngles.x, 0, 0);//beta
        Vector3 posU =  Vector3.up* bondDistance * Mathf.Abs(Mathf.Sin(prevNode.eulerAngles.x));


        Quaternion currentRotation = Quaternion.Euler(prevNode.eulerAngles);            
        Vector3 position = prevNode.position;
        position += prevNode.rotation * Vector3.forward * bondDistance;           
           
        return position;
    }

    private Quaternion nextRotation(Transform prevNode, Vector3 position)
    {          
        
        return Quaternion.LookRotation(position - prevNode.position);
    }

    private void FollowPrev(Transform prevNode, Transform node)
    {             
        Quaternion targetRotation = Quaternion.LookRotation(node.position - prevNode.position);                         
        node.rotation = Quaternion.Slerp(node.rotation, targetRotation, Time.deltaTime * bondDamping);

        Vector3 targetPosition = prevNode.position;
        Vector3 pos = node.transform.rotation * Vector3.forward * bondDistance;       
        
        targetPosition += pos;           
        node.position = Vector3.Lerp(node.position, targetPosition, Time.deltaTime * bondDamping);
    }
  
    private void HookFollowLast(Transform prevNode, Transform hook)
    {
        float targetRotationAngleY = prevNode.eulerAngles.y;
        float currentRotationAngleY = hook.transform.eulerAngles.y;           
        currentRotationAngleY = Mathf.LerpAngle(currentRotationAngleY, targetRotationAngleY, bondDamping * Time.deltaTime);
        

        float targetRotationAngleX = prevNode.eulerAngles.x;    
        float currentRotationAngleX = hook .transform.eulerAngles.x;
        currentRotationAngleX = Mathf.LerpAngle(currentRotationAngleX, targetRotationAngleX, bondDamping * Time.deltaTime);

        // Convert the angle into a rotation
        Quaternion currentRotation = Quaternion.Euler(currentRotationAngleX, currentRotationAngleY, 0);       
        hook.transform.position = prevNode.position;            
        hook.transform.LookAt(prevNode);

    }

    private void SpawnHook(Vector3 _position, Quaternion _rotation)
    {
        RpcServerSpawnHook(_position, _rotation);
    }

    [Command]
    private void RpcServerSpawnHook(Vector3 _position, Quaternion _rotation, NetworkConnectionToClient conn = null)
    {
        GameObject hook = Instantiate(hookPrefab, _position, _rotation); //create the hook object  
        NetworkServer.Spawn(hook, conn);
    }

    // private void SpawnHookNode(Vector3 _position, Quaternion _rotation, NetworkIdentity identity)
    // {
    //     NetworkConnection clientConn = identity.connectionToClient;
    //     GameObject hookNodeClone = Instantiate(hookNodePrefab, _position, _rotation);    
    //     hookNodeClone.GetComponent<HookNode>().localConn = clientConn;            
    //     NetworkServer.Spawn(hookNodeClone, clientConn);
    // }

    [Command]
    private void CmdSpawnHookNode(Vector3 _position, Quaternion _rotation, NetworkConnectionToClient conn = null)
    {
        GameObject hookNodeClone = Instantiate(hookNodePrefab, _position, _rotation);    
        hookNodeClone.GetComponent<HookNode>().localConn = conn;   
        NetworkServer.Spawn(hookNodeClone, conn);
    }
}