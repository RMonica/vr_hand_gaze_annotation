using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PointCloudExporter;
using static UnityEditor.FilePathAttribute;
using Unity.VisualScripting;
using System.Drawing;
using static ONSPPropagationMaterial;
using UnityEngine.UIElements;
using System;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using UnityEngine.Assertions.Must;

public class SetBoxes : MonoBehaviour
{
    // Variabili

    // cubo dinamico
    public GameObject CubeReshapeable;
    public static MeshRenderer meshrenderer;

    //oggetti della scena
    GameObject thisObject;
    public GameObject PointCloudParentGameObject;
    public GameObject PointCloudImporter;
    public GameObject PointCloud;
    public PointCloudGenerator point_cloud_generator;
    GameObject newBoundingBox;
    public MenuController menuController;
    public ModeController modeController;

    // vettore unitario per tener conto della scala
    Vector3 ones = new Vector3(1.0f, 1.0f, 1.0f);

    // trasformazione relativa della box
    Matrix4x4 tf_rel_box;
    // trasformazione della box
    Matrix4x4 tf_box;
    // orientamento right controller 
    Quaternion right_rot;
    // posizione right controller 
    Vector3 right_pos;


    private int selected_color = 1;
    int current_label;
    public static float threshold_buttons = 0.7f;
    public static float threshold_thumbstick = 0.5f;

    // stato dei tasti
    bool A_pressed = false;
    //bool B_pressed = false;
    bool box_present = false;
    bool r_hand_middle_trigger_pushed = false;
    bool r_hand_index_trigger_pushed = false;
    bool r_thumbstick_in_use = false;
    bool labelling_started = true;
    bool vertical_thumbstick_in_use = false;
   

    //flag annotazone iniziata
    bool label_changed = true;

    // Start is called before the first frame update
    void Start()
    {
        //all'inizio viene selezionato il colore nero sul canvas
        menuController.setSelected(selected_color);
    }

    // Update is called once per frame
    void Update()
    {
        OVRInput.Update();

        PointCloudParentGameObject = GameObject.FindWithTag("PointCloudSecretTag");
        PointCloud = GameObject.Find("PointCloud");
        PointCloudImporter = GameObject.Find("PointCloudImporter");
        point_cloud_generator = PointCloudImporter.GetComponent<PointCloudGenerator>();
        
        //controlla che non ci sia l'eye tracking attivo
        if (modeController.activeMode == ActiveMode.BoxAnnotation)
        {
            

            right_rot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
            right_pos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

            // Se è stato confermato un colore viene creato un Game Object con il colore selezionato
            if (label_changed)
            {
                if (newBoundingBox)
                    Destroy(newBoundingBox);

                newBoundingBox = GameObject.CreatePrimitive(PrimitiveType.Cube);

                newBoundingBox.transform.position = new Vector3(0f, 0f, 0f);
                newBoundingBox.transform.localScale = new Vector3(0.000001f, 0.000001f, 0.000001f);
                newBoundingBox.transform.rotation = new Quaternion(0, 0, 0, 0);
                newBoundingBox.transform.SetParent(PointCloudParentGameObject.transform, true);

                meshrenderer = newBoundingBox.GetComponent<MeshRenderer>();
                if(selected_color == 0)
                {
                    meshrenderer.material.color = UnityEngine.Color.white;
                }
                else
                {
                    meshrenderer.material.color = menuController.colorlist[selected_color - 1];

                }
                meshrenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                meshrenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                meshrenderer.material.SetInt("_ZWrite", 0);
                meshrenderer.material.DisableKeyword("_ALPHATEST_ON");
                meshrenderer.material.DisableKeyword("_ALPHABLEND_ON");
                meshrenderer.material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                meshrenderer.material.renderQueue = 3000;

                label_changed = false;
            }
            // se è stato premuto il bottone A e si è scelto un colore si crea una box grande quanto il cubo che si ha tra le mani
            if (OVRInput.Get(OVRInput.RawButton.A) && !A_pressed && labelling_started)
            {
                UnityEngine.Color color;
                if(selected_color == 0)
                {
                    color = UnityEngine.Color.white;
                }
                else
                {
                    color = menuController.colorlist[selected_color - 1];
                }
                if (newBoundingBox.GetComponent<MeshRenderer>().material.color == color)
                {
                    newBoundingBox = ResizeBox(newBoundingBox);
                    meshrenderer = newBoundingBox.GetComponent<MeshRenderer>();
                    current_label = selected_color;
                    meshrenderer.material.color = color;
                }

                // if box_present is true, the user overwrote the previous placed box
                if(box_present)
                {
                    point_cloud_generator.IncrementBoundingBoxOverwritesCount();
                }

                box_present = true;
                Debug.Log("Box set for label: " + selected_color.ToString());
                newBoundingBox.GetComponent<MeshRenderer>().enabled = true;
                A_pressed = true;
            }
            else if (!OVRInput.Get(OVRInput.RawButton.A) && A_pressed)
            {
                A_pressed = false;
            }

            // se è già stata creata una box con il bottone A e si preme il pulsante trigger con il medio della mano destra la box viene spostata in base alla rototraslazione della mano destra
            if (OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > threshold_buttons && !r_hand_middle_trigger_pushed && box_present)
            {
                thisObject = newBoundingBox;
                tf_rel_box = Matrix4x4.Inverse(
                    Matrix4x4.TRS(right_pos,
                    right_rot,
                    ones))
                    * Matrix4x4.TRS(thisObject.transform.position, thisObject.transform.rotation, ones);
                r_hand_middle_trigger_pushed = true;
            }
            else if (OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > threshold_buttons && r_hand_middle_trigger_pushed && box_present)
            {
                tf_box = Matrix4x4.TRS(right_pos,
                    right_rot,
                    new Vector3(1.0f, 1.0f, 1.0f)) * tf_rel_box;

                thisObject.transform.rotation = QuaternionFromMatrix(tf_box);
                thisObject.transform.position = PositionFromMatrix(tf_box);
            }
            // nel momento in cui si smette di premere il suddetto trigger la box verrà rilasciata nella posizione desiderata
            else if (OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) == 0.0f && r_hand_middle_trigger_pushed && box_present)
            {
                r_hand_middle_trigger_pushed = false;
                point_cloud_generator.IncrementBoundingBoxRotationsCount();
            }

            // se si preme il trigger con l'indice della mano destra e c'è una box in scena vuol dire che l'utente è soddisfatto della box creata quindi viene aggiornato il flag che permette di iniziare l'annotazione con il colore scelto.
            if ((OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > threshold_buttons) && !r_hand_index_trigger_pushed && box_present)
            {
                r_hand_index_trigger_pushed = true;
            }
            /* Non appena l'utente smette di premere il trigger con l'indice della mano destra ed è iniziata una nuova annotazione
             * si controllano tutti i punti della point cloud. Se un punto è all'interno della box creata dall'utente verranno colorati
             * (annotati) con il colore selezionato precedentemente. Successivamente viene eliminata la box.
             */
            else if ((OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) < threshold_buttons) && r_hand_index_trigger_pushed && box_present)
            {

                BoxCollider coll = newBoundingBox.GetComponent<BoxCollider>();
                List<int> list = new List<int>();
                Debug.Log(point_cloud_generator.point_cloud_info.vertices.Length.ToString());

                float halfX = (coll.size.x * 0.5f);
                float halfY = (coll.size.y * 0.5f);
                float halfZ = (coll.size.z * 0.5f);

                for (int index = 0; index <= point_cloud_generator.point_cloud_info.vertices.Length - 1; index++)
                {
                    Vector3 sphere_position = point_cloud_generator.point_cloud_info.vertices[index] * point_cloud_generator.scale;

                    sphere_position = coll.transform.InverseTransformPoint(PointCloud.transform.TransformPoint(sphere_position)) - coll.center;

                    if (sphere_position.x < halfX && sphere_position.x > -halfX &&
                       sphere_position.y < halfY && sphere_position.y > -halfY &&
                       sphere_position.z < halfZ && sphere_position.z > -halfZ) list.Add(index);


                }
                
                point_cloud_generator.SetControlPointWithLabel(list, 0, current_label);
                box_present = false;
                r_hand_index_trigger_pushed = false;
                Debug.Log("Coloring terminated.");
                newBoundingBox.GetComponent<MeshRenderer>().enabled = false;

            }

            // controlli che permettono di scorrere tra i colori presenti sul canvas
            if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[0] > threshold_thumbstick) && (selected_color != 0) && (selected_color < menuController.colorlist.Count) && !r_thumbstick_in_use)
            {
                selected_color++;
                menuController.setSelected(selected_color);
                r_thumbstick_in_use = true;
                label_changed = true;

            }
            else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[0] < -threshold_thumbstick) && selected_color > 1 && !r_thumbstick_in_use)
            {

                selected_color--;
                menuController.setSelected(selected_color);
                r_thumbstick_in_use = true;
                label_changed = true;
            }
            else if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).magnitude < threshold_thumbstick && r_thumbstick_in_use)
            {
                r_thumbstick_in_use = false;
            }

            if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] > threshold_thumbstick) && (selected_color > 4) && (selected_color <= menuController.colorlist.Count) && !vertical_thumbstick_in_use)
            {
                
                selected_color -= 4;
                menuController.setSelected(selected_color);
                vertical_thumbstick_in_use = true;
                label_changed = true;

            }
            else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] > threshold_thumbstick) && (selected_color == 0) && !vertical_thumbstick_in_use)
            {

                selected_color = 8;
                menuController.setSelected(selected_color);
                vertical_thumbstick_in_use = true;
                label_changed = true;

            }

            else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] < -threshold_thumbstick) && selected_color != 0 && selected_color < 5 && !vertical_thumbstick_in_use)
            {

                selected_color +=  4;
                menuController.setSelected(selected_color);
                vertical_thumbstick_in_use = true;
                label_changed = true;
            }
            else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] < -threshold_thumbstick) && selected_color > 4 && !vertical_thumbstick_in_use)
            {
                selected_color = 0;
                menuController.setSelected(selected_color);
                vertical_thumbstick_in_use = true;
                label_changed = true;

            }
            else if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).magnitude < threshold_thumbstick && vertical_thumbstick_in_use)
            {
                vertical_thumbstick_in_use = false;
            }
        }
    }

    // Metodo che ridimensiona la box in base alla box che l'utente ha tra le mani
    GameObject ResizeBox(GameObject tempObject)
    {
        tempObject.transform.position = CubeReshapeable.transform.position;
        tempObject.transform.rotation = CubeReshapeable.transform.rotation;
        tempObject.transform.localScale = CubeReshapeable.transform.localScale;
        return tempObject;
    }
    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Make quaternion from pose
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    static public Vector3 PositionFromMatrix(Matrix4x4 m)
    {
        return new Vector3(m[12], m[13], m[14]);
    }

}
