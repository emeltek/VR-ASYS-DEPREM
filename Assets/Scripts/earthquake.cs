using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class earthquake : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject building;
    private Transform transform;
    public float speed = 2f;
    private bool direction = false;
    void Start()
    {
        
        transform = building.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
       
        if (Input.GetKey(KeyCode.Space))
        {
            if (direction)
            {
                building.transform.Translate(Vector3.right * speed * Time.deltaTime);
                StartCoroutine(DelayAction());
            }
            
            building.transform.Translate(Vector3.right * speed * Time.deltaTime * -1);

        }
    }
    IEnumerator DelayAction()
    {
        yield return new WaitForSeconds(1f);
    }
}
