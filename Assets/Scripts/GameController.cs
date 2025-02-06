using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    void Start()
    {
        // layer UI and layer AABB
        Physics.IgnoreLayerCollision(5, 6);
        //Point cloud sphere and layer AABB
        Physics.IgnoreLayerCollision(6, 8);
        //Point cloud sphere and big sphere
        Physics.IgnoreLayerCollision(8, 9);
        //Big sphere and layer AABB
        Physics.IgnoreLayerCollision(6, 9);
    }
}
