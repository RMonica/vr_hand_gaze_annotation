using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigSphereController : MonoBehaviour
{
    [SerializeField]
    private float deltaTime = 1.5f;

    public GameObject pointCloudSphere;

    private float timer;

    private int counter;

    void Start()
    {
        timer = 0.0f;
        counter = 0;
    }

    void Update()
    {
        if (counter > 0) timer -= Time.deltaTime;

        if (timer < 0.0f)
        {
            // counter threshold
            if (counter > 40)
            {
                Destroy(this.gameObject);
            }

            counter = 0;
        }
    }

    public void Destroy()
    {
        if (counter == 0) timer = deltaTime;

        counter++;        
    }
}
