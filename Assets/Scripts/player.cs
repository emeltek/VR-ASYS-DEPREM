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
        StartCoroutine(waiter());
    }

    // Update is called once per frame
    void Update()
    {
       
    }


  private void OnTriggerEnter(Collider other)
    {
        if (shakeGroundObject.isShaking == true)
        {
            if (other.CompareTag("Cover"))
            {
                animator.SetBool("EQ", true);
                animator.SetBool("Run",false);
            }
        }
    }
    IEnumerator waiter()
    { 
        float waitTime = 4;
        yield return new WaitForSeconds(3f);
        //Rotate 90 deg
        animator.SetBool("Run",true);
    }


    
    
}
