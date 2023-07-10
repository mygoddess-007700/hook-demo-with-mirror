using Mirror;
using TMPro;
using UnityEngine;

public class NameSetter : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _input;

    private void Awake()
    {
        _input.onEndEdit.AddListener(_input_OnEndEdit);
    }

    private void _input_OnEndEdit(string text)
    {
        PlayerPrefs.SetString("DEMO_NAME", text);
        NetworkClient.localPlayer.GetComponent<PlayerController>().CmdSetName(text);
    }
} 
 