using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuItemController : MonoBehaviour
{
    public MenuController menuController;

    public GameObject leftEyeInteractor;
    public GameObject rightEyeInteractor;

    private EyeTrackingRay leftRay;
    private EyeTrackingRay rightRay;
    public int id;

    public void SimulateHovering()
    {
        GameObject canvas = GameObject.Find("Canvas/Sample Canvas");
        if (!canvas)
        {
            Debug.Log("(MenuItemController) ERRORE: CANVAS NON TROVATO, CONTROLLA PATH");
            return;
        }
        MenuController canvasComponent = canvas.GetComponent<MenuController>();
        if (this.gameObject.name == "DeleteMenuItem")
        {
            menuController.setHovered(0);
        }
        else
        {
            menuController.setHovered(canvasComponent.colorlist.IndexOf(this.gameObject.GetComponent<Image>().color) + 1);
        }
    }


    public void SimulateOnMouseDown()
    {
        OnMouseDown();
    }

    private void OnMouseDown()
    {
        //Change the selected color
        GameObject canvas = GameObject.Find("Canvas/Sample Canvas");
        if (!canvas)
        {
            Debug.Log("(MenuItemController) ERRORE: CANVAS NON TROVATO, CONTROLLA PATH");
            return;
        }
        MenuController canvasComponent = canvas.GetComponent<MenuController>();

        if (this.gameObject.name == "DeleteMenuItem")
        {
            if (canvasComponent.selectedcolor != Color.white)
            {
                canvasComponent.selectedcolor = Color.white;
                menuController.setSelected(0);
                int colorIndex = canvasComponent.colorlist.FindIndex(0, canvasComponent.colorlist.Count, match: a => a == canvasComponent.selectedcolor) + 1;
            }
        }
        else
        {
            Color gameObjectColor = this.gameObject.GetComponent<Image>().color;
            if (canvasComponent.selectedcolor != gameObjectColor)
            {
                canvasComponent.selectedcolor = gameObjectColor;
                int colorIndex = canvasComponent.colorlist.FindIndex(0, canvasComponent.colorlist.Count, match: a => a == canvasComponent.selectedcolor) + 1;
                menuController.setSelected(colorIndex);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        leftRay = leftEyeInteractor.GetComponent<EyeTrackingRay>();
        rightRay = rightEyeInteractor.GetComponent<EyeTrackingRay>();
        
        // On selection
        this.gameObject.GetComponent<Button>().onClick.AddListener(changeRayColor);
    }

    // Gets label color
    void changeRayColor()
    {
        Debug.Log("Changing Color");
        leftRay.rayColorDefaultState = this.gameObject.GetComponent<Image>().color;
        rightRay.rayColorDefaultState = this.gameObject.GetComponent<Image>().color;
    }
}
