using PointCloudExporter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudSphere : EyeInteractable
{
    public int id;

    public PointCloudGenerator point_cloud_generator;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        OnHover(IsHovered);
    }

    public override void Select(bool state, Transform anchor = null)
    {
        OnSelected(state, anchor);
    }

    public void OnSelected(bool isit, Transform anchor = null)
    {
        point_cloud_generator.OnCloudSelected(isit, anchor);
    }

    public void OnHover(bool isit)
    {
        if (isit)
        {
            point_cloud_generator.OnPointHover(id);
            point_cloud_generator.setActive(true);
        }
        else
            point_cloud_generator.OnPointPassive(id);
    }

    private void OnMouseDown()
    {
        SphereClicked(10);
    }

    public void SphereClicked(int label_weight)
    {
        Debug.Log("Sfera Grossa Cliccata!");

        point_cloud_generator.SetControlPoint(id, label_weight);
    }
}
