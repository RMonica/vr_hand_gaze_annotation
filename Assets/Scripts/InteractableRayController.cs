using JetBrains.Annotations;
using PointCloudExporter;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

//[RequireComponent(typeof(LineRenderer))]
public class InteractableRayController : MonoBehaviour
{
    //enum ActiveMode
    //{
    //    Controller,
    //    EyeTracking
    //};

    [SerializeField]
    private float rayDistance = 5.0f;

    [SerializeField]
    private float rayWidth = 0.01f;

    [SerializeField]
    private LayerMask layersForRender;

    [SerializeField]
    private LayerMask layersToInclude;

    [SerializeField]
    private LayerMask layersToIncludeHighPrioriy;

    [SerializeField]
    private Color rayColorDefaultState;

    [SerializeField]
    private OVRHand handUsedForPinchSelection;

    [SerializeField]
    private OVRControllerHelper controllerHelper;

    [SerializeField]
    private bool mockHandPinchGesture;

    public Transform rayTransform;

    [SerializeField]
    private Transform leftControllerTransform;

    //[SerializeField]
    public GameObject leftEyeInteractor;

    //[SerializeField]
    public GameObject rightEyeInteractor;

    [SerializeField]
    private float timeForMenuSelection = 1.5f;

    private float timeForMenuSelectionLeft;

    private bool intercepting;

    private bool allowPinchSelection;

    GameObject PointCloudImporter;
    public PointCloudGenerator point_cloud_generator;

    private Transform transformRightController;
    private Transform transformLeftController;

    private Transform transformLeftEye;
    private Transform transformRightEye;

    public LineRenderer lineRenderer;

    private Dictionary<int, EyeInteractable> interactables = new Dictionary<int, EyeInteractable>();

    private EyeInteractable lastEyeInteractable;
    private EyeInteractable pinchedEyeInteractable;

    private EyeInteractable lastControllerInteractable;
    private EyeInteractable pinchedControllerInteractable;

    private List<Quaternion> leftEyeRotations = new List<Quaternion>();
    private List<Quaternion> rightEyeRotations = new List<Quaternion>();

    // UI color selection (0 for delete, 1 to 8 for colors)
    private int selected_color = 1;
    public static float threshold_buttons = 0.7f;
    public static float threshold_thumbstick = 0.5f;
    bool r_thumbstick_in_use = false;
    bool vertical_thumbstick_in_use = false;

    public MenuController menuController;

    public ModeController modeController;

    // ray color
    private Color selected = Color.black;

    private void Start()
    {
        PointCloudImporter = GameObject.Find("PointCloudImporter");
        point_cloud_generator = PointCloudImporter.GetComponent<PointCloudGenerator>();


        // point_cloud_generator.eye_tracking_active == true
        if (modeController.activeMode == ActiveMode.EyeTracking) {
            lineRenderer = GetComponent<LineRenderer>();
            allowPinchSelection = (handUsedForPinchSelection != null);
            transformLeftEye = leftEyeInteractor.GetComponent<Transform>();
            transformRightEye = rightEyeInteractor.GetComponent<Transform>();

            timeForMenuSelectionLeft = timeForMenuSelection;

            Vector3 offset = (transformRightEye.position - transformLeftEye.position) / 2;

            this.transform.SetPositionAndRotation(transformLeftEye.position + offset, transformRightEye.rotation);

            SetupRay();
        }

        // point_cloud_generator.controller_raycast_active == true
        if (modeController.activeMode == ActiveMode.Controller)
        {
            lineRenderer = GetComponent<LineRenderer>();
            allowPinchSelection = false;

            timeForMenuSelectionLeft = timeForMenuSelection;
        }
    }

    private void SetupRay()
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.startColor = rayColorDefaultState;
        lineRenderer.endColor = rayColorDefaultState;
        lineRenderer.SetPosition(0, this.transform.position);
        lineRenderer.SetPosition(1, new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z + rayDistance));
    }

    private void Update()
    {
        // point_cloud_generator.eye_tracking_active == true
        if (modeController.activeMode == ActiveMode.EyeTracking)
        {
            UpdateSelection();

            // clear all hover selections when no intercepting
            if (!intercepting)
            {
                lineRenderer.startColor = lineRenderer.endColor = rayColorDefaultState;
                lineRenderer.startWidth = lineRenderer.endWidth = rayWidth;
                lineRenderer.SetPosition(1, new Vector3(0, 0, transform.position.z + rayDistance));

                //HoverEnded();
            }
        }

        // point_cloud_generator.controller_raycast_active == true
        if (modeController.activeMode == ActiveMode.Controller)
        {
            UpdateSelection();

            if (!intercepting)
            {
                //HoverEnded();
            }
        }
    }

    private void FixedUpdate()
    {
        OVRInput.FixedUpdate();

        ColorSelection();

        // point_cloud_generator.eye_tracking_active == true
        if (modeController.activeMode == ActiveMode.EyeTracking) {

            // hover ended
            HoverEnded();

            //if (IsPinching()) return;

            Vector3 offset = (transformRightEye.position - transformLeftEye.position) / 2;

            Vector3 versorLeft = Vector3.zero;
            Vector3 versorRight = Vector3.zero;

            rayWidth = offset.x * 2;

            // Store rotations of last 20 frames
            rightEyeRotations.Add(transformRightEye.rotation);
            leftEyeRotations.Add(transformLeftEye.rotation);

            const int rotations_to_store = 20;

            if (rightEyeRotations.Count > rotations_to_store) rightEyeRotations.RemoveAt(0);
            if (leftEyeRotations.Count > rotations_to_store) leftEyeRotations.RemoveAt(0);

            // Compute medium rotation between both eyes
            for (int i = 0; i < rightEyeRotations.Count; i++)
            {
                versorLeft += rightEyeRotations[i] * (new Vector3(0, 0, 1));
                versorRight += leftEyeRotations[i] * (new Vector3(0, 0, 1));
            }

            Vector3 versorMed = (versorLeft + versorRight) / (2 * rightEyeRotations.Count);

            Quaternion rotation = Quaternion.LookRotation(versorMed, transformRightEye.up);

            /*
            Quaternion rotation = new Quaternion((transformLeftEye.rotation.x + transformRightEye.rotation.x) / 2,
                                                 (transformLeftEye.rotation.y + transformRightEye.rotation.y) / 2,
                                                 (transformLeftEye.rotation.z + transformRightEye.rotation.z) / 2,
                                                 (transformLeftEye.rotation.w + transformRightEye.rotation.w) / 2);
            */

            this.transform.SetPositionAndRotation(transformLeftEye.position + offset, rotation);

            Vector3 rayDirection = this.transform.TransformDirection(Vector3.forward);
            Vector3 ray_visual_direction = rayDirection * rayDistance;

            GameObject hit = null;

            // check if origin is already within object
            EyeInteractable[] eye_interactables = FindObjectsOfType<EyeInteractable>();
            intercepting = false;

            for (int i = 0; i < 2 && !intercepting; i++)
            {
                LayerMask current_layer_mask = (i == 0) ? layersToIncludeHighPrioriy : layersToInclude;

                foreach (EyeInteractable ei in eye_interactables)
                {
                    if (!ei.isActiveAndEnabled)
                        continue;
                    if (!ei.GetComponent<Collider>())
                        continue;
                    if ((current_layer_mask & (1 << ei.gameObject.layer)) == 0) // layer is ignored
                        continue;
                    if (ei.GetComponent<Collider>().ClosestPoint(this.transform.position) == this.transform.position)
                    {
                        intercepting = true;
                        hit = ei.gameObject;
                        break;
                    }
                }

                // Check if eye ray intersects with any objects included in the layersToInclude
                if (!intercepting)
                {
                    intercepting = Physics.Raycast(this.transform.position, rayDirection, out RaycastHit my_hit, Mathf.Infinity, current_layer_mask);
                    if (intercepting)
                        hit = my_hit.transform.gameObject;
                }

                if (intercepting)
                {
                    if (hit.transform.CompareTag("MenuItem"))
                    {
                        timeForMenuSelectionLeft -= Time.deltaTime;
                        hit.transform.GetComponent<MenuItemController>().SimulateHovering();

                        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                        {
                            hit.transform.GetComponent<MenuItemController>().SimulateOnMouseDown();
                        }

                        if (timeForMenuSelectionLeft <= 0)
                        {
                            //hit.transform.GetComponent<Button>().onClick.Invoke();

                            //hit.transform.GetComponent<MenuItemController>().SimulateOnMouseDown(); # RIMOSSA SELEZIONE TEMPORALE
                            
                            timeForMenuSelectionLeft = timeForMenuSelection;
                        }
                    }
                    //else if (hit.transform.CompareTag("PointCloudSphere") && leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState != Color.black)
                    else if (hit.transform.gameObject.GetComponent<EyeInteractable>() != null)
                    {
                        timeForMenuSelectionLeft = timeForMenuSelection;

                        // keep a cache of interactables
                        if (!interactables.TryGetValue(hit.transform.gameObject.GetHashCode(), out EyeInteractable eyeInteractable))
                        {
                            eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
                            interactables.Add(hit.transform.gameObject.GetHashCode(), eyeInteractable);
                        }

                        // limit line render ray
                        var toLocalSpace = transform.InverseTransformPoint(eyeInteractable.transform.position);
                        lineRenderer.SetPosition(1, new Vector3(0, 0, toLocalSpace.z));

                        lastEyeInteractable = eyeInteractable;

                        // hover started
                        eyeInteractable.Hover(true, null, this.transform.position, rayDirection);
                    }
                }
            }

            if (!intercepting)
            {
                timeForMenuSelectionLeft = timeForMenuSelection;

                HoverEnded();
            }
        }

        // point_cloud_generator.controller_raycast_active
        if (modeController.activeMode == ActiveMode.Controller)
        {
            HoverEnded();

            GameObject hit = null;

            EyeInteractable[] eyeInteractables = FindObjectsOfType<EyeInteractable>();

            intercepting = false;

            Vector3 rayDirection = rayTransform.transform.TransformDirection(Vector3.forward);
            Vector3 ray_visual_direction = rayDirection * rayDistance;

            for (int i = 0; i < 2 && !intercepting; i++)
            {
                LayerMask current_layer_mask = (i == 0) ? layersToIncludeHighPrioriy : layersToInclude;

                foreach (EyeInteractable ei in eyeInteractables)
                {
                    if (!ei.isActiveAndEnabled)
                        continue;
                    if (!ei.GetComponent<Collider>())
                        continue;
                    if ((current_layer_mask & (1 << ei.gameObject.layer)) == 0) // layer is ignored
                        continue;
                    if (ei.GetComponent<Collider>().ClosestPoint(rayTransform.transform.position) == rayTransform.transform.position)
                    {
                        intercepting = true;
                        hit = ei.gameObject;
                        break;
                    }
                }

                // Check if eye ray intersects with any objects included in the layersToInclude
                if (!intercepting)
                {
                    intercepting = Physics.Raycast(rayTransform.transform.position, rayDirection, out RaycastHit my_hit, Mathf.Infinity, current_layer_mask);
                    
                    if (intercepting)
                    {
                        //_messages.text = leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState.ToString();
                        hit = my_hit.transform.gameObject;

                        Vector3 controllerPos = rayTransform.position;
                        
                        lineRenderer.SetPosition(0, controllerPos);
                        lineRenderer.SetPosition(1, my_hit.point);
                        lineRenderer.startColor = lineRenderer.endColor = selected;
                    }
                    else
                    {
                        Vector3 controllerPos = rayTransform.position;
                        Quaternion controllerRot = rayTransform.rotation;

                        if (Physics.Raycast(rayTransform.transform.position, rayDirection, out RaycastHit hit_, Mathf.Infinity, layersForRender))
                        {
                            lineRenderer.SetPosition(0, controllerPos);
                            lineRenderer.SetPosition(1, hit_.point);
                            lineRenderer.startColor = lineRenderer.endColor = selected;
                        }
                        else
                        {
                            Ray ray = new Ray(controllerPos, controllerRot * Vector3.forward);
                            lineRenderer.SetPosition(0, controllerPos);
                            lineRenderer.SetPosition(1,ray.GetPoint(rayDistance*10));
                            lineRenderer.startColor = lineRenderer.endColor = selected;
                        }
                    }
                }

                if (intercepting)
                {
                    if (hit.transform.CompareTag("MenuItem"))
                    {
                        timeForMenuSelectionLeft -= Time.deltaTime;
                        hit.transform.GetComponent<MenuItemController>().SimulateHovering();

                        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                        {
                            hit.transform.GetComponent<MenuItemController>().SimulateOnMouseDown();
                        }

                        if (timeForMenuSelectionLeft <= 0)
                        {
                            // hit.transform.GetComponent<MenuItemController>().SimulateOnMouseDown(); # RIMOSSA SELEZIONE TEMPORALE

                            //hit.GetComponent<Button>().onClick.Invoke();
                            timeForMenuSelectionLeft = timeForMenuSelection;
                        }
                        lineRenderer.startColor = lineRenderer.endColor = selected = menuController.selectedcolor;
                    }
                    //else if (hit.transform.CompareTag("PointCloudSphere") && leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState != Color.black)
                    else if (hit.transform.gameObject.GetComponent<EyeInteractable>() != null)
                    {
                        timeForMenuSelectionLeft = timeForMenuSelection;

                        // keep a cache of interactables
                        if (!interactables.TryGetValue(hit.transform.gameObject.GetHashCode(), out EyeInteractable eyeInteractable))
                        {
                            eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
                            interactables.Add(hit.transform.gameObject.GetHashCode(), eyeInteractable);
                        }

                        // limit line render ray
                        //var toLocalSpace = transform.InverseTransformPoint(eyeInteractable.transform.position);
                        //lineRenderer.SetPosition(1, new Vector3(0, 0, toLocalSpace.z));
                        //lineRenderer.SetPosition(1, rayTransform.position);
                        

                        lastEyeInteractable = eyeInteractable;

                        // hover started
                        eyeInteractable.Hover(true, null, rayTransform.transform.position, rayDirection);
                    }
                }

                if (!intercepting)
                {
                    HoverEnded();
                }
            }

        }
    }

    private void ColorSelection()
    {
        if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[0] > threshold_thumbstick) && (selected_color != 0) && (selected_color < menuController.colorlist.Count) && !r_thumbstick_in_use)
        {
            selected_color++;
            menuController.selectedcolor = menuController.colorlist[selected_color - 1];
            menuController.setSelected(selected_color);

            rightEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];
            leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];

            r_thumbstick_in_use = true;
        }
        else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[0] < -threshold_thumbstick) && selected_color > 1 && !r_thumbstick_in_use)
        {
            selected_color--;
            menuController.selectedcolor = menuController.colorlist[selected_color];
            menuController.setSelected(selected_color);

            rightEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];
            leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];

            r_thumbstick_in_use = true;
        }
        else if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).magnitude < threshold_thumbstick && r_thumbstick_in_use)
        {
            r_thumbstick_in_use = false;
        }

        if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] > threshold_thumbstick) && (selected_color > 4) && (selected_color <= menuController.colorlist.Count) && !vertical_thumbstick_in_use)
        {
            selected_color -= 4;
            menuController.selectedcolor = menuController.colorlist[selected_color];
            menuController.setSelected(selected_color);

            rightEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];
            leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];

            vertical_thumbstick_in_use = true;

        }
        else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] > threshold_thumbstick) && (selected_color == 0) && !vertical_thumbstick_in_use)
        {
            selected_color = 8;
            menuController.selectedcolor = menuController.colorlist[selected_color - 1];
            menuController.setSelected(selected_color);

            rightEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];
            leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];

            vertical_thumbstick_in_use = true;

        }

        else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] < -threshold_thumbstick) && selected_color != 0 && selected_color < 5 && !vertical_thumbstick_in_use)
        {
            selected_color += 4;
            menuController.selectedcolor = menuController.colorlist[selected_color - 1];
            menuController.setSelected(selected_color);

            rightEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];
            leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = menuController.colorlist[selected_color - 1];

            vertical_thumbstick_in_use = true;
        }
        else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] < -threshold_thumbstick) && selected_color > 4 && !vertical_thumbstick_in_use)
        {
            selected_color = 0;
            menuController.selectedcolor = Color.white;
            menuController.setSelected(selected_color);

            rightEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = Color.white; // Color.HSVToRGB(0.0f, 1.0f, 0.5f);
            leftEyeInteractor.GetComponent<EyeTrackingRay>().rayColorDefaultState = selected = Color.white; // Color.HSVToRGB(0.0f, 1.0f, 0.5f);

            vertical_thumbstick_in_use = true;

            UnityEngine.Debug.Log("Delete tool");

        }
        else if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).magnitude < threshold_thumbstick && vertical_thumbstick_in_use)
        {
            vertical_thumbstick_in_use = false;
        }
     }

    private void UpdateSelection()
    {
        //Debug.Log(allowPinchSelection);
        //Debug.Log(modeController.activeMode);
        
        //if (handUsedForPinchSelection.GetFingerIsPinching(OVRHand.HandFinger.Index))
        //    Debug.Log("-----------------------------------------------------");

        if (IsPinching())
        {
            if (lastEyeInteractable != null && pinchedEyeInteractable == null)
            {
                //if (modeController.activeMode == ActiveMode.EyeTracking)
                //    lastEyeInteractable.Select(true, handUsedForPinchSelection.IsTracked ?
                //        handUsedForPinchSelection.transform : transform);
                //if (modeController.activeMode == ActiveMode.Controller)
                //    lastEyeInteractable.Select(true, leftControllerTransform.transform);

                if ((modeController.activeMode == ActiveMode.Controller) || (modeController.activeMode == ActiveMode.EyeTracking))
                {
                    lastEyeInteractable.Select(true, leftControllerTransform.transform);
                    point_cloud_generator.IncrementPointCloudRotationsCount();
                }

                pinchedEyeInteractable = lastEyeInteractable;
            }
        }
        else
        {
            if (pinchedEyeInteractable != null)
            {
                pinchedEyeInteractable.Select(false);
            }
            pinchedEyeInteractable = null;
        }
    }

    private void OnDestroy() => interactables.Clear();

    // used to rotate the point cloud
    private bool IsPinching()
    {
        //if (modeController.activeMode == ActiveMode.EyeTracking && handUsedForPinchSelection.GetFingerIsPinching(OVRHand.HandFinger.Index))
        //    return true;

        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) && ((modeController.activeMode == ActiveMode.Controller) || (modeController.activeMode == ActiveMode.EyeTracking)))
            return true;

        return false;
    }

    private void HoverEnded(bool reset = false)
    {
        foreach (var interactable in interactables) interactable.Value.Hover(false);
        interactables.Clear();
    }
}
