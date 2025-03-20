using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Door : NetworkBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 2f; // Max distance to open the door

    public Quaternion closedRotation;
    public Quaternion openRotation;

    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Coroutine doorAnimationCoroutine;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, openAngle, 0));

        // Sync door state when it changes
        isOpen.OnValueChanged += (oldValue, newValue) => StartDoorAnimation(newValue);
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            RequestToggleDoorServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestToggleDoorServerRpc(ServerRpcParams rpcParams = default)
    {
         if (!IsPlayerNearby()) return; // Prevent toggling if no player is close
    
    // Log the current state
    Debug.Log($"Toggling door state. Current state: {isOpen.Value}");

    isOpen.Value = !isOpen.Value;  // Toggle the state

    // Log the new state
    Debug.Log($"New door state: {isOpen.Value}");
    }

    public bool IsPlayerNearby()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player")) // Ensure player objects have the "Player" tag
            {
                Debug.Log(isOpen.Value);
                return true;
            }
        }
        return false;
    }

    public void StartDoorAnimation(bool open)
    {
        if (doorAnimationCoroutine != null)
        {
            StopCoroutine(doorAnimationCoroutine);
        }
        doorAnimationCoroutine = StartCoroutine(AnimateDoor(open));
    }

    public IEnumerator AnimateDoor(bool open)
    {
        Quaternion targetRotation = open ? openRotation : closedRotation;
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);
            yield return null;
        }
        transform.rotation = targetRotation; // Ensure exact final rotation
    }
}