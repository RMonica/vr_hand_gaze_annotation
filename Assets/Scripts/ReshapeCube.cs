using PointCloudExporter;
using UnityEngine;
//script relativo al game Object CubeReshapeable
public class ReshapeCube : MonoBehaviour
{
    // vettore che tiene traccia della rotazione della box
    Vector3 rotation = new Vector3(0.0f, 0.0f, 0.0f);

    // dots at Touch Controllers
    public GameObject DotRight;
    public GameObject DotLeft;

    public GameObject PointCloudImporter;
    public PointCloudGenerator point_cloud_generator;
    public ModeController mode_controller;
    bool cube_reshapeable_active = true;
    private void Start()
    {
        PointCloudImporter = GameObject.Find("PointCloudImporter");
        point_cloud_generator = PointCloudImporter.GetComponent<PointCloudGenerator>();
        if (mode_controller.activeMode != ActiveMode.BoxAnnotation)
        {
            cube_reshapeable_active = false;
            DestroyImmediate(this);
        }    
    }

    private void Update()
    {
        if (cube_reshapeable_active)
        {
            rotation = DotRight.transform.rotation.eulerAngles;

            transform.localScale = Quaternion.Inverse(Quaternion.Euler(rotation)) * (DotLeft.transform.position - DotRight.transform.position);
            transform.position = (DotRight.transform.position + DotLeft.transform.position) / 2.0f; //la posizione del bounding box è il punto medio tra i due dot
            transform.rotation = Quaternion.Euler(rotation); //rotazione calcolata prima
        }
        

    }
}

