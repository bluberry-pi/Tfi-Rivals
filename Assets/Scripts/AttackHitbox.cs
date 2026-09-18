using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private bool alreadyHit = false;
    private GameObject owner;

    private void Awake()
    {
        owner = transform.root.gameObject;
    }

    private void OnEnable()
    {
        alreadyHit = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (alreadyHit)
            return;

        // Don't hit yourself
        if (other.transform.root.gameObject == owner)
            return;

        PlayerHealth target = other.GetComponentInParent<PlayerHealth>();

        if (target == null)
            return;

        target.TakeHit();

        alreadyHit = true;

        Debug.Log(
            owner.name + " hit " +
            target.gameObject.name
        );
    }
}