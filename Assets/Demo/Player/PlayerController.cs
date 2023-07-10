using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerController : NetworkBehaviour
{
    [Header("Input System")] 
    [SerializeField] private InputActionReference _moveWithKeyBoard;
    [SerializeField] private InputActionReference _lookWithKeyBoard;
    [SerializeField] private InputActionReference _shootWithKeyBoard;

    [Header("Rotation Settings")]
    public float moveSpeed = 5f;

    public InputActionReference ShootWithKeyBoard => _shootWithKeyBoard;

    public MeshRenderer meshRenderer;

    public TMP_Text Text;
    
    [SyncVar(hook = nameof(SetColor))]
    public Color32 color = Color.black;
    
    [SyncVar(hook = nameof(SetName))]
    public string name = "test";

    private void OnEnable()
    {
        _moveWithKeyBoard.action.Enable();
        _lookWithKeyBoard.action.Enable();
        _shootWithKeyBoard.action.Enable();
    }

    private void OnDisable()
    {
        _moveWithKeyBoard.action.Disable();
        _lookWithKeyBoard.action.Disable();
        _shootWithKeyBoard.action.Disable();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Color demoColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        string demoName = "UnSet";
        if (PlayerPrefs.HasKey("DEMO_NAME"))
        {
            demoName = PlayerPrefs.GetString("DEMO_NAME");
        }
        name = demoName;
        color = demoColor;
    }

    void SetColor(Color32 _, Color32 newColor)
    {
        meshRenderer.material.color = newColor;
    }

    void SetName(string oldName, string newName)
    {
        Text.text = newName;
    }

    [Command]
    public void CmdSetName(string value, NetworkConnectionToClient conn = null)
    {
        name = value;
    }
    
    private void Update()
    {
        if (!isOwned) return;

        ProcessMove();

        ProcessRotate();
    }

    private void ProcessRotate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ClientPlayerReference.instance.left.isPressed)
        {
            Quaternion q = Quaternion.identity;
            q *= Quaternion.AngleAxis(-1, Vector3.up);
            transform.rotation *= q;
        }
        else if (ClientPlayerReference.instance.right.isPressed)
        {
            Quaternion q = Quaternion.identity;
            q *= Quaternion.AngleAxis(1, Vector3.up);
            transform.rotation *= q;
        }
#endif
        
        var turnActivated = _lookWithKeyBoard.action.IsPressed();

        if (turnActivated)
        {
            var rotate = _lookWithKeyBoard.action.ReadValue<Vector2>();
            Quaternion q = Quaternion.identity;
            q *= Quaternion.AngleAxis(rotate.x, Vector3.up);
            transform.rotation *= q;
        }
    }

    private void ProcessMove()
    {
        var input = _moveWithKeyBoard.action.ReadValue<Vector2>();
        var inputMove = Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);
        var translationInWorldSpace = transform.TransformDirection(inputMove);

        transform.position += translationInWorldSpace * moveSpeed * Time.deltaTime;
    }
}