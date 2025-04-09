using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject testDevice;

    [SerializeField] private GameObject ml2Device;

    [SerializeField] private GameObject htc2Device;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameObject deviceInstance = Instantiate(GetPlayerPrefab(), transform);
        }
    }

    private GameObject GetPlayerPrefab() {
        if (Application.isEditor) return testDevice;

        if (SystemInfo.deviceModel == "Magic Leap Magic Leap 2")
            return ml2Device;

        return htc2Device;
    }
}
