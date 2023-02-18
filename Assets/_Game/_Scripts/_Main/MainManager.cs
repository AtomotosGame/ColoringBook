using UnityEngine;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    public Camera cameraObj;
    public MenuObject coloringMenu, paintingMenu;
    public GameObject settingMenu;
    public GameObject returnBtn;
    public GameObject coloringReturnBtn;
    public GameObject coloringSelectedMenu;

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
        OnMenuButtonClicked(PlayerPrefs.GetInt("isPainting", 0) == 1);
    }

    public void OnMenuButtonClicked(bool isPainting)
    {
        PlayerPrefs.SetInt("isPainting", isPainting ? 1 : 0);
        PlayerPrefs.Save();

        // paintingMenu.menu.SetActive(isPainting);
        // coloringMenu.menu.SetActive(!isPainting);

        // cameraObj.backgroundColor = isPainting ? paintingMenu.color : coloringMenu.color;
        // paintingMenu.image.sprite = isPainting ? paintingMenu.onEnableSprite : paintingMenu.onDisableSprite;
        // coloringMenu.image.sprite = !isPainting ? coloringMenu.onEnableSprite : coloringMenu.onDisableSprite;
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