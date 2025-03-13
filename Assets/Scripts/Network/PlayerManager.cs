using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject testDevice;
    [SerializeField] private GameObject ml2Device;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameObject devicePrefab = Application.isEditor ? testDevice : ml2Device;
            GameObject deviceInstance = Instantiate(devicePrefab, transform);

        }
    }
}
