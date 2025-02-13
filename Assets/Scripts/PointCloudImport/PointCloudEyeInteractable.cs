using PointCloudExporter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudEyeInteractable : EyeInteractable
{
    public PointCloudGenerator point_cloud_generator;
    public ModeController mode_controller;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (IsHovered)
        {
            // point_cloud_generator.eye_tracking_active
            if (mode_controller.activeMode == ActiveMode.EyeTracking)
            { 
                point_cloud_generator.OnInterceptingEyeRay(hovered_ray_origin, hovered_ray_direction);
                point_cloud_generator.setActive(true);
            }
            else if (mode_controller.activeMode == ActiveMode.Controller)
            {
                point_cloud_generator.OnInterceptingEyeRay(hovered_ray_origin, hovered_ray_direction);
                point_cloud_generator.setActive(true);
            }
        }
        else
        {
            point_cloud_generator.ClearHoverMask();
        }
    }

    public override void Select(bool isit, Transform anchor = null)
    {
        point_cloud_generator.OnCloudSelected(isit, anchor);
    }
}
