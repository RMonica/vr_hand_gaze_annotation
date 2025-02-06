using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum ActiveMode
{
    Controller,
    EyeTracking,
    BoxAnnotation
};

public enum SelectedScene
{
    Airplane,
    Workbench,
    Cone,
    WorkbenchGT,
    ConeGT
};

public class ModeController : MonoBehaviour
{
    public SelectedScene selectedScene;
    public ActiveMode activeMode;
    public string userCode = "";
    public Camera main_camera = null;

    // Start is called before the first frame update
    void Start()
    {
        if (main_camera != null)
        {
            main_camera.nearClipPlane = 0.1f;
        }
        //if (activeMode == ActiveMode.EyeTracking)
        //{
        //    this.GetComponent<TextMeshProUGUI>().SetText("Eye tracking is active\r\n\r\nShift your gaze to the Menu to choose a label color\r\n\r\nShift your gaze to the Point Cloud to label the pointed area\r\n\r\nUse left thumbstick to scroll through label colors\r\n\r\nPress left middle-finger to move the point cloud");
        //}
        //else if (activeMode == ActiveMode.BoxAnnotation)
        //{
        //    this.GetComponent<TextMeshProUGUI>().SetText("Box Annotation is active\r\n\r\nPress B to select a label color\r\n\r\nPress A to set a new box\r\n\r\nPress left middle-finger trigger to move the box\r\n\r\nPress right index-finger trigger to label the area inside the box\r\n\r\nUse left thumbstick to scroll through label colors\r\n\r\nPress left middle-finger to move the point cloud");
        //}
        //else if (activeMode == ActiveMode.Controller)
        //{
        //    this.GetComponent<TextMeshProUGUI>().SetText("Controller Mode is active\r\n\r\nPoint the controller to the Menu to choose a label\r\n\r\nPoint the controller to the point cloud and press button A to label it\r\n\r\nUse left thumbstick to scroll through label colors\r\n\r\nPress left middle-finger to move the point cloud");
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
