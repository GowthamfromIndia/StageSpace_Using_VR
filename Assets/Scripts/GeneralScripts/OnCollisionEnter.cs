using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCollision : MonoBehaviour
{
    // Called when the character collides with something
    void OnCollisionEnter(Collision collision)
    {
        // Check if the character collides with a specific object
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Character collided with an enemy!");
            // Handle damage, knockback, or other logic
        }
    }
}
