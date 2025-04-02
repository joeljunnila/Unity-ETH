using Unity.Netcode;
using UnityEngine;

public class NetworkedDoor : NetworkBehaviour
{
    private Animator doorAnimator;
    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    void Start()
    {
        doorAnimator = GetComponent<Animator>();
        isOpen.OnValueChanged += OnDoorStateChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void OpenDoorServerRpc()
    {
        isOpen.Value = true;
    }

    void OnDoorStateChanged(bool oldState, bool newState)
    {
        if (newState)
        {
            doorAnimator.SetTrigger("Open");
        }
    }
}