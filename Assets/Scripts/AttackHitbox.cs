using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private bool alreadyHit = false;

    private void OnEnable()
    {
        alreadyHit = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (alreadyHit)
            return;

        // Find the player who owns the collider that was hit
        PlayerHealth target = other.GetComponentInParent<PlayerHealth>();

        if (target == null)
            return;

        // Check which player this hitbox belongs to
        GameObject attacker = transform.root.gameObject;

        // Prevent hitting oneself
        if (target.gameObject == attacker)
            return;

        if (attacker.name.StartsWith("Mahesh", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Mahesh hit Pawan");
        }
        else if (attacker.name.StartsWith("Pawan", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Pawan hit Mahesh");
        }

        target.TakeHit();

        alreadyHit = true;
    }
}