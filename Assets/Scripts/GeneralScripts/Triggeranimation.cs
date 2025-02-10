using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAnimation : MonoBehaviour
{
    public Animator characterAnimator; // Reference to the Animator
    public string animationToPlay = "Cheering While Sitting"; // The name of the animation you want to play

    void OnTriggerEnter(Collider other)
    {
        // Check if the other object has the tag or name you want (e.g., "Player")
        if (other.CompareTag("Player"))
        {
            // Play the animation when the player enters the trigger radius
            characterAnimator.SetTrigger(animationToPlay);
        }
    }
}
