using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//using UnityEngine.UIElements;

public class MenuController : MonoBehaviour
{
    public GameObject deleteButton;

    [SerializeField]
    private GameObject menuItemPrefab;

    [SerializeField]
    private GameObject leftEyeInteractor;

    [SerializeField]
    private GameObject rightEyeInteractor;

    [SerializeField]
    private int numberOfItems;

    private Vector3 initialPoint;

    // Color list, index corresponds to label value
    // NOTE: indexes go from 0 (Color.black) to 7 (Color.gray), where colorlabel goes from 1 to 8, for some reason :(
    // when colorlabel is equal to 0 it means that we have selected the remove tool
    //WARNING: this list is copy-pasted in SphereClustering.cs and MenuItemController.cs to obtain same distribution for selection
    public List<Color> colorlist = new() { Color.black, Color.blue, Color.green, Color.red, Color.cyan, Color.yellow, Color.magenta, Color.gray };

    // ray color, black at the beginning and when delete function is selected
    public Color selectedcolor = Color.black;

    // pointcloud label index (from 1 to 8), reflects the UI color
    public int colorlabel = 1;

    // green boxes conturing the selectd color in the UI
    private List<GameObject> hiddenhighlights = new();

    // white boxes conturing the hovered color in the UI
    private List<GameObject> hiddenhoverings = new();

    //private bool selectedDelete = false;

    // index of selected color (0 for delete, 1 to 8 for colors)
    private int selectedid = -1;
    // index of hovered color (0 for delete, 1 to 8 for colors)
    private int hoveredid = -1;

    // Start is called before the first frame update
    void Start()
    {
        RectTransform canvasRectTransform = GetComponent<RectTransform>();
        RectTransform prefabObjectTransform = menuItemPrefab.GetComponent<RectTransform>();

        float buttonPerRow = (canvasRectTransform.rect.width - prefabObjectTransform.rect.width) / (prefabObjectTransform.rect.width + 20);
        int rows = Mathf.CeilToInt(numberOfItems / buttonPerRow);
        int itemsLeft = numberOfItems;

        float offsetW = prefabObjectTransform.rect.width + 20;
        float offsetH = prefabObjectTransform.rect.height + 20;

        initialPoint.x =  - canvasRectTransform.rect.width/2 + 150;
        initialPoint.y = canvasRectTransform.rect.height/2 - prefabObjectTransform.rect.height/2 - 60;
        initialPoint.z = 0;
        
        //OLD: for used whiteboard dimension to generate random colors, now colors are pre-determined
        //for (int i = 0; i < rows; i++)
        for(int i = 0; i<2; ++i)
        {
            //for (int j = 0; j < (int)Mathf.Min(buttonPerRow, itemsLeft); j++)
            for (int j = 0; j < 4; ++j)
            {

                GameObject go_hiddenhovering = Instantiate(menuItemPrefab);
                go_hiddenhovering.transform.SetParent(this.transform);
                go_hiddenhovering.GetComponent<RectTransform>().localPosition = new Vector3(initialPoint.x + j * offsetW, initialPoint.y - i * offsetH, initialPoint.z + 1);
                go_hiddenhovering.GetComponent<RectTransform>().localScale = new Vector3(1.1f, 1.1f, 1f);
                go_hiddenhovering.GetComponent<Image>().color = Color.white; 
                go_hiddenhovering.GetComponent<MenuItemController>().leftEyeInteractor = leftEyeInteractor;
                go_hiddenhovering.GetComponent<MenuItemController>().rightEyeInteractor = rightEyeInteractor;
                go_hiddenhovering.SetActive(false);
                go_hiddenhovering.GetComponent<MenuItemController>().menuController = this;
                hiddenhoverings.Add(go_hiddenhovering);
                 
                GameObject go_hiddenhighlight = Instantiate(menuItemPrefab);
                go_hiddenhighlight.transform.SetParent(this.transform);
                go_hiddenhighlight.GetComponent<RectTransform>().localPosition = new Vector3(initialPoint.x + j * offsetW, initialPoint.y - i * offsetH, initialPoint.z + 1);
                go_hiddenhighlight.GetComponent<RectTransform>().localScale = new Vector3(1.1f, 1.1f, 1f);
                go_hiddenhighlight.GetComponent<Image>().color = new Color(0.5f, 1.0f, 0.5f);
                go_hiddenhighlight.GetComponent<MenuItemController>().leftEyeInteractor = leftEyeInteractor;
                go_hiddenhighlight.GetComponent<MenuItemController>().rightEyeInteractor = rightEyeInteractor;
                go_hiddenhighlight.SetActive(false);
                go_hiddenhighlight.GetComponent<MenuItemController>().menuController = this;
                hiddenhighlights.Add(go_hiddenhighlight);


                GameObject go = Instantiate(menuItemPrefab);
                go.transform.SetParent(this.transform);
                go.GetComponent<RectTransform>().localPosition = new Vector3(initialPoint.x + j * offsetW, initialPoint.y - i * offsetH, initialPoint.z);
                go.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
                //go.GetComponent<Image>().color = Random.ColorHSV(0.0f, 1.0f, 0.0f, 1.0f, 0.5f, 1.0f, 1.0f, 1.0f);
                go.GetComponent<Image>().color = colorlist[i*4+j];
                go.GetComponent<MenuItemController>().leftEyeInteractor = leftEyeInteractor;
                go.GetComponent<MenuItemController>().rightEyeInteractor = rightEyeInteractor;
                go.GetComponent<MenuItemController>().menuController = this;

                
            }

            itemsLeft -= (int)buttonPerRow;
        }

        setSelected(1); // select first button
    }

    private void FixedUpdate()
    {
        // reset UI selection of the color
        // reset delete button color
        deleteButton.GetComponent<Image>().color = Color.black;
        // reset white boxes over UI colors
        for (int i = 0; i < hiddenhoverings.Count; ++i)
        {
            hiddenhoverings[i].SetActive(false);
        }
        // reset green boxes over UI colors
        for (int i = 0; i < hiddenhighlights.Count; ++i)
        {
            hiddenhighlights[i].SetActive(false);
        }

        // change color of delete button based on UI color selection and hovering state
        if (selectedid == 0)
        {
            deleteButton.GetComponent<Image>().color = Color.red;
        }
        else if (hoveredid== 0)
        {
            deleteButton.GetComponent<Image>().color = Color.HSVToRGB(0.0f, 1.0f, 0.5f);
        }

        // after clearing all the boxes, draw the green and white boxes over the selected and
        // hovered color, respectively
        if (selectedid > 0)
        {
            hiddenhighlights[selectedid - 1].SetActive(true);
        }
        if(hoveredid > 0 )
        {
            hiddenhoverings[hoveredid - 1].SetActive(true);
        }

        hoveredid = -1;
    }

    public void setHovered(int id)
    {
        hoveredid = id;

        /*
        // Deactivate all the hovering highlighters
        if (!selectedDelete)
        {
            deleteButton.GetComponent<Image>().color = Color.black;
        }
        for (int i = 0; i < hiddenhoverings.Count; ++i)
        {
            hiddenhoverings[i].SetActive(false);
        }

        //Activate the right hovering highlighter o change delete button color
        if (id == 0 && !selectedDelete)
        {
            deleteButton.GetComponent<Image>().color = Color.HSVToRGB(0.0f, 1.0f, 0.5f);
        }
        else if (id != 0)
        {
            hiddenhoverings[id-1].SetActive(true);
        }
        */
    }

    public void setSelected(int id)
    {
        selectedid = id;
        colorlabel = id;

        if (id == 0) Debug.Log("Delete tool");
        Debug.Log("Chosen color index: " + id);

        /*
        // Deactivate all the selection highlighters
        deleteButton.GetComponent<Image>().color = Color.black;
        for (int i = 0; i < hiddenhighlights.Count; ++i)
        {
            hiddenhighlights[i].SetActive(false);
            selectedDelete = false;
        }
        for (int i = 0; i < hiddenhoverings.Count; ++i)
        {
            hiddenhoverings[i].SetActive(false);
        }

        //Activate the right selection highlighter o change delete button color
        if (id == 0)
        {
            deleteButton.GetComponent<Image>().color = Color.red;
            selectedDelete = true;
        }
        else
        {            
            hiddenhighlights[id - 1].SetActive(true);
        }
        */
    }
}
