using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class EyeTrackingRay : MonoBehaviour
{
    [SerializeField]
    private float rayDistance = 1.0f;

    [SerializeField]
    private float rayWidth = 0.01f;

    [SerializeField]
    private LayerMask layersToInclude;

    public Color rayColorDefaultState = Color.yellow;

    private LineRenderer lineRenderer;

    private Dictionary<int, EyeInteractable> interactables = new Dictionary<int, EyeInteractable>();

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupRay();
    }

    private void SetupRay()
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.startColor = rayColorDefaultState;
        lineRenderer.endColor = rayColorDefaultState;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, new Vector3(transform.position.x, transform.position.y, transform.position.z + rayDistance));
    }

    private void Update()
    {
        // clear all hover selections when no intercepting
        lineRenderer.startColor = lineRenderer.endColor = rayColorDefaultState;
        lineRenderer.SetPosition(1, new Vector3(0, 0, transform.position.z + rayDistance));
    }

    private void OnDestroy() => interactables.Clear();
}
