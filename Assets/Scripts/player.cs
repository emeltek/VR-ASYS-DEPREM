using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    // Start is called before the first frame update
    Animator animator;
    public ShakeGroundObject shakeGroundObject;
  
    void Start()
    {
        animator = GetComponent<Animator>();
        
    }

    public void  StartRunAnimation()
    {
        animator.SetBool("Run", true);
    }

  private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cover"))
        {
            animator.SetBool("EQ", true);
            animator.SetBool("Run",false);
        }
    }
   


    
    
}
