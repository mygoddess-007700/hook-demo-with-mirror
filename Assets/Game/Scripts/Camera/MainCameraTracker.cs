using Mirror;

public class MainCameraTracker : NetworkBehaviour
{
    public MainCamera AttachedCamera;

    public bool IsAttached => AttachedCamera != null;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        MainCamera.Instance.Attach(this);
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        if (MainCamera.Instance.AttachedTo == this)
            MainCamera.Instance.Attach(null);
    }

    private void OnDisable()
    {
        if (AttachedCamera)
            AttachedCamera = null;
    }
}
