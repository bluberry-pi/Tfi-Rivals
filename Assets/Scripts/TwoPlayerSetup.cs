using UnityEngine;
using UnityEngine.InputSystem;

public class TwoPlayerSetup : MonoBehaviour
{
    public GameObject maheshPrefab;
    public GameObject pawanPrefab;

    public Transform maheshSpawn;
    public Transform pawanSpawn;

    void Start()
    {
        if (Gamepad.all.Count < 2)
        {
            Debug.LogError("Two controllers are required!");
            return;
        }

        // Controller 1 → Mahesh
        PlayerInput mahesh = PlayerInput.Instantiate(
            maheshPrefab,
            playerIndex: 0,
            pairWithDevice: Gamepad.all[0]
        );

        mahesh.transform.position = maheshSpawn.position;


        // Controller 2 → Pawan
        PlayerInput pawan = PlayerInput.Instantiate(
            pawanPrefab,
            playerIndex: 1,
            pairWithDevice: Gamepad.all[1]
        );

        pawan.transform.position = pawanSpawn.position;
    }
}