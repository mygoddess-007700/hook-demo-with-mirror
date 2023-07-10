using System.Collections;
using Mirror;
using UnityEngine;

public class ShootHook : NetworkBehaviour
{
    public PlayerController Controller;     
    public GameObject HookLeaderPrefab;     
    public float ShootInterval = 0.2f;
    
    private float ShootTime;
    private Transform mTransform;
       
    public bool shootEnd = false;
    public bool canShoot = true;

    public override void OnStartClient()
    {
        base.OnStartClient();
        mTransform = transform;
    }

    private void OnDrawGizmosSelected()
    {
        Debug.DrawRay(transform.position, transform.forward * 20.0f, Color.blue);
    }

    private void Update()
    {
        if (!isOwned)
        {
            return;
        }

        if (shootEnd)
        {
            shootEnd = false;
            StartCoroutine(WaitForNextShoot(ShootInterval));
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ClientPlayerReference.instance.shoot.isPressed)
        {
            Debug.Log($"canShoot: {canShoot}");
            if (!canShoot)
            {
                return;
            }

            canShoot = false;
            Shoot();
        }
#else
        if (Controller.ShootWithKeyBoard.action.IsPressed())
        {
            if (!canShoot)
            {
                return;
            }

            canShoot = false;
            Shoot();
        }
#endif
    }

    private IEnumerator WaitForNextShoot(float shootInterval)
    {
        yield return new WaitForSeconds(shootInterval);
        canShoot = true;
    }

    public void Shoot()
    {
        RpcServerSpawnHookLeader();
    }

    [Command]
    private void RpcServerSpawnHookLeader(NetworkConnectionToClient conn = null)
    {
        GameObject hookLeaderObject = Instantiate(HookLeaderPrefab, mTransform.position, mTransform.rotation); 
        NetworkServer.Spawn(hookLeaderObject, conn);
    }
}