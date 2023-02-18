using UnityEngine;
using UnityEngine.UI;
using DanielLochner.Assets.SimpleScrollSnap;

public class MainManager : MonoBehaviour
{
    public Camera cameraObj;
    public MenuObject coloringMenu, paintingMenu;
    public GameObject settingMenu;
    public GameObject returnBtn;
    public GameObject coloringReturnBtn;
    public GameObject coloringSelectedMenu;
    public Transform ScrolllistColoringObj;
    [SerializeField] public SimpleScrollSnap scrollSnap;
    [SerializeField] public Toggle togglePrefab;
    [SerializeField] public GameObject panelPrefab;
    [SerializeField] public ToggleGroup toggleGroup;

    [System.Serializable]
    public class MenuObject
    {
        public GameObject menu;
        public Color color;
        public Image image;
        public GameObject obj;
        public Sprite onEnableSprite;
        public Sprite onDisableSprite;
    }

    void Awake()
    {
        Camera.main.aspect = 16 / 9f;
    }

    void Start()
    {
        OnMenuButtonClicked();
    }

    public void OnMenuButtonClicked()
    {
        int isColoring =  PlayerPrefs.GetInt("isColoring");

        if (isColoring > 0){
            coloringSelectedMenu.SetActive(true);
            coloringMenu.obj.SetActive(false);  
            paintingMenu.obj.SetActive(false);  
            onClickColoringItem();
            ScrollListManagerColoring.selectedcolorItem = isColoring;
            DynamicContent dynamicClass = new DynamicContent();
            dynamicClass.toggleWidth = (togglePrefab.transform as RectTransform).sizeDelta.x * (Screen.width / 2048f); ;
            dynamicClass.ScrolllistColoringObj = ScrolllistColoringObj;
            dynamicClass.scrollSnap = scrollSnap;
            dynamicClass.togglePrefab = togglePrefab;
            dynamicClass.panelPrefab = panelPrefab;
            dynamicClass.toggleGroup = toggleGroup;
            dynamicClass.createColoringPanels();
        } else if (isColoring == 0) {
            OnDrawButtonClick();
        } 


        // paintingMenu.menu.SetActive(isColoring);
        // coloringMenu.menu.SetActive(!isColoring);

        // cameraObj.backgroundColor = isColoring ? paintingMenu.color : coloringMenu.color;
        // paintingMenu.image.sprite = isColoring ? paintingMenu.onEnableSprite : paintingMenu.onDisableSprite;
        // coloringMenu.image.sprite = !isColoring ? coloringMenu.onEnableSprite : coloringMenu.onDisableSprite;
    }

    public void PlaySoundClick()
    {
        MusicController.USE.PlaySound(MusicController.USE.clickSound);
    }

    public void OnSettingButtonClick() {
        settingMenu.SetActive(!settingMenu.activeSelf);
    }

    public void OnDrawButtonClick() {
        
        if (PlayerPrefs.GetInt("allDrawItem") == 0 && PlayerPrefs.GetInt("firstDraw") != 1) {
            PlayerPrefs.SetInt("firstDraw", 1);
            ScrollListManagerColoring.saveIndexStringStatic = "PaintingList";
            ScrollListManagerColoring.LoadGame(0);
        } else {
            paintingMenu.menu.SetActive(!paintingMenu.menu.activeSelf);
            paintingMenu.obj.SetActive(!paintingMenu.obj.activeSelf);
            coloringMenu.obj.SetActive(!coloringMenu.obj.activeSelf);
            returnBtn.SetActive(!returnBtn.activeSelf);
        }
    }
    public void OnColorButtonClick() {
        coloringMenu.menu.SetActive(!coloringMenu.menu.activeSelf);
        paintingMenu.obj.SetActive(!paintingMenu.obj.activeSelf);
        coloringMenu.obj.SetActive(!coloringMenu.obj.activeSelf);
        returnBtn.SetActive(!returnBtn.activeSelf);
    }
    public void OnReturnButtonClick(bool coloringButton) {
        if (coloringButton) {
            coloringMenu.menu.SetActive(true);
            paintingMenu.menu.SetActive(false);
            coloringSelectedMenu.SetActive(false);
            coloringReturnBtn.SetActive(false);
            returnBtn.SetActive(true);
        } 
        else
        {
            coloringMenu.menu.SetActive(false);
            paintingMenu.menu.SetActive(false);
            coloringSelectedMenu.SetActive(false);
            paintingMenu.obj.SetActive(true);
            coloringMenu.obj.SetActive(true);
            returnBtn.SetActive(!returnBtn.activeSelf);
        }
    }

    public void onClickColoringItem() {
        coloringReturnBtn.SetActive(true);
    }
}