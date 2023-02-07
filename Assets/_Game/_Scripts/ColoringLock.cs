using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColoringLock : MonoBehaviour
{

    public GameObject lockImage;
    public GameObject priceBtn;
    public GameObject coloringSelectedMenu;
    public GameObject coloringMenu;
    public bool free;

    // Start is called before the first frame update
    void Start()
    {
        if (!free) {
            lockImage.SetActive(false);
            priceBtn.SetActive(false);
        }  
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void onClickItem(int index) {
        ScrollListManagerColoring.selectedcolorItem = index;
        coloringSelectedMenu.SetActive(true);
        coloringMenu.SetActive(false);

    }

}
