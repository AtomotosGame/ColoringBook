using UnityEngine;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    public Camera cameraObj;
    public MenuObject coloringMenu, paintingMenu;

    [System.Serializable]
    public class MenuObject
    {
        public GameObject menu;
        public Color color;
        public Image image;
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

        paintingMenu.menu.SetActive(isPainting);
        coloringMenu.menu.SetActive(!isPainting);

        cameraObj.backgroundColor = isPainting ? paintingMenu.color : coloringMenu.color;
        paintingMenu.image.sprite = isPainting ? paintingMenu.onEnableSprite : paintingMenu.onDisableSprite;
        coloringMenu.image.sprite = !isPainting ? coloringMenu.onEnableSprite : coloringMenu.onDisableSprite;
    }

    public void PlaySoundClick()
    {
        MusicController.USE.PlaySound(MusicController.USE.clickSound);
    }
}