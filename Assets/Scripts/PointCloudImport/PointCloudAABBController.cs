using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudAABBController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void CreateAABB(float maxX, float maxY, float maxZ, float minX, float minY, float minZ, Transform transform)
    {
        BoxCollider boxCollider = this.gameObject.GetComponent<BoxCollider>();
        
        float dimX = maxX - minX;
        float dimY = maxY - minY;
        float dimZ = maxZ - minZ;

        float radiusX = dimX / 2;
        float radiusY = dimY / 2;
        float radiusZ = dimZ / 2;

        Vector3 aabbCenter = new Vector3(minX + radiusX, minY + radiusY, minZ + radiusZ);
        
        boxCollider.size = new Vector3(dimX, dimY, dimZ);
        boxCollider.center = aabbCenter;
    }
}
