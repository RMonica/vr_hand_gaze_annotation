using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Script utile per controllare le collisioni della point cloud
public class CollisionDetection : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Point Cloud in collision with object: " +  collision.gameObject.name);
    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("In collision with: " + collision.gameObject.name);

    }

    private void OnCollisionExit(Collision collision)
    {
        //Debug.Log("Exit collision with: " + collision.gameObject.name);

    }

}
