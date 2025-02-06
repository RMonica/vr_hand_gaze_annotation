using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EyeInteractable : MonoBehaviour
{
    protected bool IsHovered { get; private set; } = false;

    protected Vector3 hovered_ray_origin;
    protected Vector3 hovered_ray_direction;

    public void Hover(bool state)
    {
        IsHovered = state;
    }

    public void Hover(bool state, Material newMaterial, Vector3 ray_origin, Vector3 ray_direction)
    {
        IsHovered = state;
        hovered_ray_origin = ray_origin;
        hovered_ray_direction = ray_direction;
    }

    public virtual void Select(bool state, Transform anchor = null)
    {
        
    }
}
