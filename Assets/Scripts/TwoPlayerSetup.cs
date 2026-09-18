using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class TwoPlayerSetup : MonoBehaviour
{
    [Header("Prefabs (Used only if not found in scene)")]
    public GameObject maheshPrefab;
    public GameObject pawanPrefab;

    [Header("Spawn Points")]
    public Transform maheshSpawn;
    public Transform pawanSpawn;

    [Header("Scene References (Optional override)")]
    public PlayerInput existingMahesh;
    public PlayerInput existingPawan;

    void Start()
    {
        // Determine available input devices
        InputDevice device1 = Gamepad.all.Count > 0 ? (InputDevice)Gamepad.all[0] : Keyboard.current;
        InputDevice device2 = Gamepad.all.Count > 1 ? (InputDevice)Gamepad.all[1] : (Gamepad.all.Count == 1 ? (InputDevice)Keyboard.current : null);

        // 1. Check if Mahesh exists in the scene
        PlayerInput mahesh = existingMahesh != null ? existingMahesh : FindPlayerInScene("Mahesh");

        // 2. Check if Pawan exists in the scene
        PlayerInput pawan = existingPawan != null ? existingPawan : FindPlayerInScene("Pawan");

        // Unpair existing devices from both players first to avoid any device collisions
        if (mahesh != null && mahesh.user.valid) mahesh.user.UnpairDevices();
        if (pawan != null && pawan.user.valid) pawan.user.UnpairDevices();

        // 3. Setup or Instantiate Mahesh
        if (mahesh == null && maheshPrefab != null)
        {
            if (device1 != null)
            {
                mahesh = PlayerInput.Instantiate(
                    maheshPrefab,
                    playerIndex: 0,
                    pairWithDevice: device1
                );
            }
            else
            {
                mahesh = PlayerInput.Instantiate(maheshPrefab, playerIndex: 0);
            }
        }
        else if (mahesh != null && device1 != null)
        {
            AssignDevice(mahesh, device1);
        }

        if (mahesh != null && maheshSpawn != null)
        {
            mahesh.transform.position = maheshSpawn.position;
        }

        // 4. Setup or Instantiate Pawan
        if (pawan == null && pawanPrefab != null)
        {
            if (device2 != null)
            {
                pawan = PlayerInput.Instantiate(
                    pawanPrefab,
                    playerIndex: 1,
                    pairWithDevice: device2
                );
            }
            else
            {
                pawan = PlayerInput.Instantiate(pawanPrefab, playerIndex: 1);
            }
        }
        else if (pawan != null && device2 != null)
        {
            AssignDevice(pawan, device2);
        }

        if (pawan != null && pawanSpawn != null)
        {
            pawan.transform.position = pawanSpawn.position;
        }
    }

    private PlayerInput FindPlayerInScene(string characterName)
    {
        // 1. Try finding by direct GameObject name
        GameObject go = GameObject.Find(characterName);
        if (go != null)
        {
            PlayerInput pi = go.GetComponent<PlayerInput>();
            if (pi != null) return pi;
        }

        // 2. Search through all active PlayerInput components in scene
        PlayerInput[] all = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (var p in all)
        {
            if (p.gameObject.name.IndexOf(characterName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return p;
        }

        return null;
    }

    private void AssignDevice(PlayerInput player, InputDevice device)
    {
        if (player == null || device == null) return;

        // Try switching control scheme to match this device
        if (!player.SwitchCurrentControlScheme(device))
        {
            if (player.user.valid)
            {
                player.user.UnpairDevices();
                InputUser.PerformPairingWithDevice(device, user: player.user);
                if (!string.IsNullOrEmpty(player.defaultControlScheme))
                {
                    player.user.ActivateControlScheme(player.defaultControlScheme);
                }
            }
        }

        player.ActivateInput();
    }
}