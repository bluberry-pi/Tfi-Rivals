using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private bool alreadyHit = false;

    private GameObject owner;

    private void Awake()
    {
        // The root player that owns this hitbox
        owner = transform.root.gameObject;
    }

    private void OnEnable()
    {
        // Every time Punch/Kick appears,
        // this becomes a fresh attack.
        alreadyHit = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // This attack already hit someone
        if (alreadyHit)
            return;

        // Don't hit our own player
        if (other.transform.root.gameObject == owner)
            return;

        // Find PlayerHealth on the other player
        PlayerHealth target = other.GetComponentInParent<PlayerHealth>();

        if (target == null)
            return;

        // Remove exactly ONE heart
        target.TakeHit();

        // Prevent this same Punch/Kick from
        // removing more hearts.
        alreadyHit = true;

        Debug.Log(
            owner.name + " hit " +
            target.gameObject.name
        );
    }
}