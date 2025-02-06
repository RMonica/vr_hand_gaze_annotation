using PointCloudExporter;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChangeText : MonoBehaviour
{

    public GameObject PointCloudImporter;
    public PointCloudGenerator point_cloud_generator;
    // Start is called before the first frame update
    void Start()
    {
        PointCloudImporter = GameObject.Find("PointCloudImporter");
        point_cloud_generator = PointCloudImporter.GetComponent<PointCloudGenerator>();

        if (point_cloud_generator.eye_tracking_active == false)
        {
            this.GetComponent<TextMeshProUGUI>().SetText("Press B to select a label color\r\n\r\nPress A to set a new box\r\n\r\nPress right middle-finger trigger to move the box\r\n\r\nPress right index-finger trigger to label the area inside the box\r\n\r\nUse right thumbstick to scroll through label colors\r\n\r\nPress left middle-finger to move the point cloud");
        }
        else
        {
            this.GetComponent<TextMeshProUGUI>().SetText("Eye tracking is active, enjoy!");

        }


    }

    // Update is called once per frame
    void Update()
    {
        

    }
}
