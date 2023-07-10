using Mirror;
using UnityEngine;

public class HookNode : NetworkBehaviour
{
    public bool wasCollided;

    private DemoPlayerReference _demoPlayerReference;
    public NetworkConnection localConn;
    
    private ContactPoint contact;
    public Quaternion transRotation = Quaternion.identity;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.isOwned)
        {
            return;
        }

        if (transRotation != Quaternion.identity)
            transform.rotation = transRotation;
        
        InitHookNode();
    }

    private void InitHookNode()
    {
        ClientPlayerReference.instance.localPlayerReferences.TryGetValue(NetworkClient.connection.identity, out _demoPlayerReference);
        System.Diagnostics.Debug.Assert(_demoPlayerReference != null, nameof(_demoPlayerReference) + " != null");
        
        GetComponent<HookEventListener>().StartTakeBackHook += _demoPlayerReference.hookLeader.StartTakeBackHook;
        Physics.IgnoreCollision(GetComponent<Collider>(),
            _demoPlayerReference.rightHand.gameObject.GetComponent<Collider>());
        Physics.IgnoreCollision(GetComponent<Collider>(),
            _demoPlayerReference.leftHand.gameObject.GetComponent<Collider>());
        Physics.IgnoreCollision(GetComponent<Collider>(),
            _demoPlayerReference.playerController.gameObject.GetComponent<Collider>());
        Physics.IgnoreCollision(GetComponent<Collider>(), _demoPlayerReference.hook.gameObject.GetComponent<Collider>());
        if (_demoPlayerReference.hookLeader.nodes.Count < _demoPlayerReference.hookLeader.MaxNode)
        {
            _demoPlayerReference.hookLeader.nodes.Add(gameObject);
        }

        if (wasCollided)
        {
            ContactPoint contact = this.contact;
            Vector3 curDir = transform.TransformDirection(Vector3.forward);

            Vector3 newDir = Vector3.Reflect(curDir, contact.normal);

            float dotValue = Vector3.Dot(contact.normal, newDir);
            float angle = Mathf.Acos(dotValue) * Mathf.Rad2Deg;
            
            if (float.IsNaN(angle) || angle <= 5.0f || angle >= 175.0f)
            {
                GetComponent<HookEventListener>().NotifyTakeBack();
            }
            else
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.forward, newDir);
            }
            
            transRotation = transform.rotation;
        }
        
        _demoPlayerReference.hookLeader.lastHookNodeSpawned = true;
    }
    

    public void RemoveMe()
    {
        CmdDespawnHookNode();
    }

    [Command]
    private void CmdDespawnHookNode(NetworkConnectionToClient conn = null)
    {
        NetworkServer.Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "wall" && !wasCollided && collision.contacts.Length != 0)
        {
            wasCollided = true;
            contact = collision.contacts[0];
        }
    }
}
