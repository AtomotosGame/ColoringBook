﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase;
  using Firebase.Extensions;
  using Firebase.Storage;

#if UNITY_WEBGL
using System.IO;
#endif

public class ScrollListManagerColoring : MonoBehaviour
{
    public string saveIndexString;
    public static string saveIndexStringStatic;
    public static int selectedcolorItem = 0;
    public static string selectedDirectoryName;
    public static string selectedFileName;

    public ColoringItem [] coloringItems;

    [System.Serializable]
    public class ColoringItem
    {
        public int fileNumber;
        public string directoryName;
        public string fileName;
    }

    [Space]
    public bool horizontalList;

    // List element size
    public float cellSizeX = 512;
    public float cellSizeY = 288;
    public float spacing = -50;

    [Space]
    public bool useButtons;
    public GameObject backwardButton;
    public GameObject forwardButton;

    private List<float> snapPositions;
    private float currentCharCheckTemp;
    private Vector3 newLerpPosition;
    private bool lerping;
    private float lerpingSpeed = 0.1f;
    private float focusedElementScale = 1.2f;
    private float unfocusedElementsScale = 0.5f;
    private List<GameObject> listOfCharacters;
    private bool buttonPressed;
    private int currentCharacter;
    private int firstPos = 0;

    private int texWidth = 1024;
    private int texHeight = 1024;

    public static Dictionary<string, Sprite> allTexturesDic;

    private void Awake()
    {
        saveIndexStringStatic = saveIndexString;

        if (allTexturesDic == null)
        {
            allTexturesDic = new Dictionary<string, Sprite>();
        }

        firstPos = PlayerPrefs.GetInt(saveIndexString, 0);

        lerping = false;
        buttonPressed = false;

        snapPositions = new List<float>();
        listOfCharacters = new List<GameObject>();

        // Get all characters and put then into list
        foreach (Transform t in transform)
            listOfCharacters.Add(t.gameObject);

        // GetFirebaseData();
        // LoadAllTexture();

        PlayerPrefs.SetInt("isColoring", -2);
        PlayerPrefs.Save();
    }

    private void SetNewPos(int num)
    {
        if (horizontalList)
        {
            newLerpPosition = new Vector3(snapPositions[num], 0, 0);
        }
        else
        {
            num = snapPositions.Count - 1 - num;
            newLerpPosition = new Vector3(0, snapPositions[num], 0);
        }

        currentCharacter = num;
        transform.localPosition = newLerpPosition;
        lerping = true;
    }

    public void LoadAllTexture()
    {

        for (int j = 0; j < transform.childCount; j++){
            for (int i = 0; i < transform.GetChild(j).childCount; i++){
                if (transform.GetChild(j).GetChild(i).transform.gameObject.activeSelf)
                    transform.GetChild(j).GetChild(i).GetComponent<Image>().sprite = LoadImage(saveIndexString + (i+j*10).ToString(), saveIndexString + (i+j*10).ToString() == ColoringBookManager.ID , 1024, 1024);
                    
            }
        }
    }

    public void LoadAllColorTexture()
    {

        for (int j = 0; j < transform.childCount; j++){
            for (int i = 0; i < transform.GetChild(j).childCount; i++){
                if (transform.GetChild(j).GetChild(i).transform.gameObject.activeSelf)
                    Debug.Log(selectedcolorItem);
                    Debug.Log(coloringItems[selectedcolorItem].directoryName);
                    Debug.Log(ColoringBookManager.ID);
                    if (selectedcolorItem == 1 ) 
                        transform.GetChild(j).GetChild(i).GetComponent<Image>().sprite = LoadImage(saveIndexString + coloringItems[selectedcolorItem].directoryName + (i+j*10).ToString(), saveIndexString + coloringItems[selectedcolorItem].directoryName  + (i+j*10).ToString() == ColoringBookManager.ID, 512,414);    
                    else
                        transform.GetChild(j).GetChild(i).GetComponent<Image>().sprite = LoadImage(saveIndexString + coloringItems[selectedcolorItem].directoryName + (i+j*10).ToString(), saveIndexString + coloringItems[selectedcolorItem].directoryName  + (i+j*10).ToString() == ColoringBookManager.ID, 512,512);
            }
        }
    }

    private Sprite LoadImage(string key, bool update = false, int width = 1024, int height = 1024)
    {
        texWidth = width;
        texHeight = height;
        if (allTexturesDic.ContainsKey(key) && !update)
        {
            return allTexturesDic[key];
        }
        else
        {
            byte[] loadPixels = new byte[texWidth * texHeight * 4];

#if UNITY_WEBGL
            string file = Application.persistentDataPath + "/Landscape" + key + ".sav";
            if (File.Exists(file))
            {
                loadPixels = System.Convert.FromBase64String(File.ReadAllText(file));
            }
            else
            {
                return null;
            }
#else
            if (PlayerPrefs.HasKey(key))
            {
                loadPixels = System.Convert.FromBase64String(PlayerPrefs.GetString(key));
            }
            else
            {
                return null;
            }
#endif

            // byte[] pixels = new byte[texWidth * texHeight * 4];
            // System.Array.Copy(pixels, new byte[texWidth * texHeight * 4], pixels.Length);
            // PlayerPrefs.SetString(key, System.Convert.ToBase64String(pixels));
            // PlayerPrefs.SetString(key, null);
            // PlayerPrefs.Save();
            if (loadPixels != null)
            {
                Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.LoadRawTextureData(loadPixels);
                tex.Apply(false);

                Sprite sp = Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), Vector2.zero, 100);

                if (allTexturesDic.ContainsKey(key))
                {
                    allTexturesDic[key] = sp;
                }
                else
                {
                    allTexturesDic.Add(key, sp);
                }

                return sp;
            }
            else
            {
                return null;
            }
        }
    }

    // This function purpouse is to wait a little before pressing button again
    private IEnumerator ButtonPressed()
    {
        yield return new WaitForSeconds(0.4f);
        buttonPressed = false;
    }

    public void BackwardButtonPressed()
    {
        if (horizontalList)
        {
            if (currentCharacter > 0 && !buttonPressed)
            {
                // Button pressed
                buttonPressed = true;

                currentCharacter -= 1;
                newLerpPosition = new Vector3(snapPositions[currentCharacter], transform.localPosition.y, 0);
                lerping = true;

                StartCoroutine(ButtonPressed());
            }
        }
        else
        {
            if (currentCharacter > 0 && !buttonPressed)
            {
                // Button pressed
                buttonPressed = true;

                currentCharacter -= 1;
                newLerpPosition = new Vector3(transform.localPosition.x, snapPositions[listOfCharacters.Count - currentCharacter - 1], 0);
                lerping = true;

                StartCoroutine(ButtonPressed());
            }
        }
    }

    public void ForwardButtonPressed()
    {
        if (horizontalList)
        {
            if (currentCharacter < snapPositions.Count - 1 && !buttonPressed)
            {
                // Button pressed
                buttonPressed = true;

                currentCharacter += 1;
                newLerpPosition = new Vector3(snapPositions[currentCharacter], transform.localPosition.y, 0);
                lerping = true;

                StartCoroutine(ButtonPressed());
            }
        }
        else
        {
            if (currentCharacter < listOfCharacters.Count - 1 && !buttonPressed)
            {
                // Button pressed
                buttonPressed = true;

                currentCharacter += 2;
                newLerpPosition = new Vector3(transform.localPosition.x, snapPositions[listOfCharacters.Count - currentCharacter], 0);
                lerping = true;

                StartCoroutine(ButtonPressed());
            }
        }
    }

    private void SetButtonActive(GameObject button)
    {
        if (!useButtons)
            return;
        Color c = button.GetComponent<Image>().color;
        c = new Color(1, 1, 1, 1);
        button.GetComponent<Image>().color = c;
        //button.transform.GetChild(0).GetComponent<Image>().color = c;

        button.GetComponent<Button>().interactable = true;
    }

    private void SetButtonInactive(GameObject button)
    {
        if (!useButtons)
            return;
        Color c = button.GetComponent<Image>().color;
        c = new Color(1, 1, 1, 0.3f);
        button.GetComponent<Image>().color = c;
        //button.transform.GetChild(0).GetComponent<Image>().color = c;

        button.GetComponent<Button>().interactable = false;
    }

    public static void LoadGame(int index)
    {
        MusicController.USE.PlaySound(MusicController.USE.clickSound);
        Debug.Log("saveIndexStringStatic " + saveIndexStringStatic);
        PlayerPrefs.SetInt(saveIndexStringStatic, index);
        PlayerPrefs.Save();

        
        if (PlayerPrefs.GetInt("allDrawItem") == null || index > PlayerPrefs.GetInt("allDrawItem") ) {
            PlayerPrefs.SetInt("allDrawItem", index);
            PlayerPrefs.Save();
        }


        if (saveIndexStringStatic == "ColoringList")
        {
            ColoringBookManager.maskTexIndex = index;
            ColoringBookManager.maskPath = "gs://decent-tracer-842.appspot.com/" + selectedDirectoryName + "/Thumbs/" + selectedFileName + (index+1).ToString() + ".png";
            ColoringBookManager.ID = saveIndexStringStatic + selectedDirectoryName + index.ToString();
            PlayerPrefs.SetInt("isColoring", selectedcolorItem); 
            PlayerPrefs.Save();
        }
        else
        {
            ColoringBookManager.maskTexIndex = -1;
            ColoringBookManager.ID = saveIndexStringStatic + index.ToString();
            PlayerPrefs.SetInt("isColoring", -1); 
            PlayerPrefs.Save();
        }



        // PlayerPrefs.SetInt(saveIndexStringStatic, 0);
        // PlayerPrefs.SetInt("allDrawItem", 0);

        // ColoringBookManager.ID = saveIndexStringStatic + index.ToString();
        Debug.Log(ColoringBookManager.ID);
        SceneManager.LoadScene("PaintScene");
    }

    public void RenamePanel() {

        for (int i = 0 ; i < transform.childCount; i++ ){
            transform.GetChild(i).transform.gameObject.name = i.ToString();
        }
    }
    
    // Draw panel remove item
    public void RemoveItems() {
        int allItemNum = PlayerPrefs.GetInt("allDrawItem");
        // PlayerPrefs.SetInt("allDrawItem", 0);
        // PlayerPrefs.SetInt("PaintingList", 0);
        // PlayerPrefs.SetInt("ColoringList", 0);
        // PlayerPrefs.SetInt("firstDraw", 0);
        int panelNum =  (int) (Mathf.Floor((allItemNum+1)/10)) + 1;
        int remainItemNum = allItemNum - (panelNum-1)*10;
   
        for (int i = 0; i < 10; i++) {
            if (i <= remainItemNum) {

            } else if (i == remainItemNum+1) {
                transform.GetChild(panelNum-1).GetChild(i).GetChild(0).GetComponent<Image>().enabled = true;
            } else {
                transform.GetChild(panelNum-1).GetChild(i).transform.gameObject.SetActive(false);
            }
        }
    }

    // color panel remote item
    public void ColoringRemoveItems() {
        int allItemNum = coloringItems[selectedcolorItem].fileNumber;
        int panelNum =  (int) (Mathf.Floor((allItemNum)/10)) + 1;
        int remainItemNum = allItemNum - (panelNum-1)*10;
        if (remainItemNum == 0) { 
            panelNum--;
            remainItemNum = 10;
        }
   
        for (int i = 0; i < 10; i++) {
            if (i < remainItemNum) {

            } else {
                transform.GetChild(panelNum-1).GetChild(i).transform.gameObject.SetActive(false);
            }
        }
    }

    public async void GetFirebaseData () {

        FirebaseStorage storage = FirebaseStorage.GetInstance("gs://decent-tracer-842.appspot.com");

        selectedDirectoryName = coloringItems[selectedcolorItem].directoryName;
        selectedFileName = coloringItems[selectedcolorItem].fileName;

        // int index = 0;
        for (int i = 0 ; i < coloringItems[selectedcolorItem].fileNumber; i++ ){
            string path = "gs://decent-tracer-842.appspot.com/" + coloringItems[selectedcolorItem].directoryName + "/Thumbs/" + coloringItems[selectedcolorItem].fileName + (i+1).ToString() + ".png";

            if (!allTexturesDic.ContainsKey(path)) {

                StorageReference reference = storage.GetReferenceFromUrl(path);

                const long maxAllowedSize = 2 * 1024 * 1024;
                
                reference.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task => {
                    if (task.IsFaulted || task.IsCanceled) {
                        Debug.LogException(task.Exception);
                        // Uh-oh, an error occurred!
                    }
                    else {
                        byte[] fileContents = task.Result;
                        Debug.Log(fileContents.Length);
                        // Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
                        // tex.filterMode = FilterMode.Point;
                        // tex.wrapMode = TextureWrapMode.Clamp;
                        // tex.LoadRawTextureData(fileContents);
                        // tex.Apply(false);
                        Texture2D texture = new Texture2D(512, 512);
                        texture.LoadImage(fileContents);

                        #if UNITY_WEBGL
                                string file = Application.persistentDataPath + "/Landscape" + path + ".sav";
                                string fileData = System.Convert.ToBase64String(fileContents);
                                File.WriteAllText(file, fileData);
                        #else
                                PlayerPrefs.SetString(path, System.Convert.ToBase64String(fileContents));
                                PlayerPrefs.Save();
                        #endif

                        Sprite sp = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 100);


                        Debug.Log(path);
                        if (fileContents != null)
                        {
                            if (allTexturesDic.ContainsKey(path))
                            {
                                allTexturesDic[path] = sp;
                            }
                            else
                            {
                                allTexturesDic.Add(path, sp);
                            }

                        }

                        string [] nameSplit = path.Split(selectedFileName);
                        Debug.Log(nameSplit);
                        string [] extensionSplit = nameSplit[nameSplit.Length-1].Split('.');
                        Debug.Log(extensionSplit[0]);
                        int index = int.Parse(extensionSplit[0]) -1;

                        int panelNum =  (int) (Mathf.Floor((index)/10));
                        
                        int itemNum = index - panelNum*10;
                        transform.GetChild(panelNum).GetChild(itemNum).GetChild(0).GetComponent<Image>().sprite = sp;

                        index++;
                    }
                });
            } else {
                int panelNum =  (int) (Mathf.Floor((i)/10));
                int itemNum = i - panelNum*10;
                transform.GetChild(panelNum).GetChild(itemNum).GetChild(0).GetComponent<Image>().sprite = allTexturesDic[path];
            }
        }
        Debug.Log("Finished");
    }

    private Sprite LoadImage1(string key, bool update = false, int width = 1024, int height = 1024)
    {
        texWidth = width;
        texHeight = height;
        if (allTexturesDic.ContainsKey(key) && !update)
        {
            return allTexturesDic[key];
        }
        else
        {
            return null;
        }
    }
}