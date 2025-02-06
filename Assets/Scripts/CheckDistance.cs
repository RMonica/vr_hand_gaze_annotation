using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

/* Script che controlla la posizione della point cloud.
 * Se la posizione della point cloud esce al di fuori del range
 * questa viene riportata nella sua posizione originale.
 */
public class CheckDistance : MonoBehaviour
{
    Vector3 pos;
    Quaternion rot;
    Vector3 new_pos;
    public GameObject table;
    // Start is called before the first frame update
    void Start()
    {

        //pos = transform.position;
        rot = transform.rotation;
        pos = table.transform.position;
        new_pos = new Vector3(pos.x, pos.y + table.GetComponent<BoxCollider>().size.y, pos.z);

        //Debug.Log("Initial rotation:" + rot.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Rigidbody>().isKinematic) // true if grabbed
            return;

        if(transform.position.x > pos.x + table.GetComponent<BoxCollider>().size.x || transform.position.x < pos.x - table.GetComponent<BoxCollider>().size.x ||
            transform.position.y > pos.y + 2 || transform.position.y < pos.y ||
            transform.position.z > pos.z + table.GetComponent<BoxCollider>().size.z || transform.position.z < pos.z - table.GetComponent<BoxCollider>().size.z)
        {
            //Debug.Log("Position of point cloud out of range");
            //Debug.Log("Position out of range:" + transform.position.ToString());
            
            transform.position = new_pos;
            transform.rotation = rot;
            //Debug.Log("Rot :" + transform.rotation.ToString());
            //Debug.Log("Initial rotation:" + rot.ToString());
            //Debug.Log("Pos :" + transform.position.ToString());
            //Debug.Log("Initial position:" + pos.ToString());

        }
        

    }
}
