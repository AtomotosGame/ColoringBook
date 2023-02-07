using UnityEngine;
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

    private int texWidth = 512;
    private int texHeight = 512;

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

        LoadAllTexture();
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
                transform.GetChild(j).GetChild(i).GetComponent<Image>().sprite = LoadImage(saveIndexString + (i+j*10).ToString(), saveIndexString + (i+j*10).ToString() == ColoringBookManager.ID);
            }
        }
    }

    private Sprite LoadImage(string key, bool update = false)
    {
        if (allTexturesDic.ContainsKey(key) && !update)
        {
            return allTexturesDic[key];
        }
        else
        {
            byte[] loadPixels = new byte[texWidth * texHeight * 9];

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
        }


        if (saveIndexStringStatic == "ColoringList")
        {
            ColoringBookManager.maskTexIndex = index;
        }
        else
        {
            ColoringBookManager.maskTexIndex = -1;
        }

        // PlayerPrefs.SetInt(saveIndexStringStatic, 0);
        // PlayerPrefs.SetInt("allDrawItem", 0);

        ColoringBookManager.ID = saveIndexStringStatic + index.ToString();
        Debug.Log(ColoringBookManager.ID);
        SceneManager.LoadScene("PaintScene");
    }

    public void RenamePanel() {

        for (int i = 0 ; i < transform.childCount; i++ ){
            transform.GetChild(i).transform.gameObject.name = i.ToString();
        }
    }
    
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

    public void GetFirebaseData () {
        // Get a reference to the storage service, using the default Firebase App
        // FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        // Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        //     var dependencyStatus = task.Result;
        //     if (dependencyStatus == Firebase.DependencyStatus.Available) {
        //         // Create and hold a reference to your FirebaseApp,
        //         // where app is a Firebase.FirebaseApp property of your application class.
        //         Debug.Log(Firebase.FirebaseApp.DefaultInstance);
        //         // app = Firebase.FirebaseApp.DefaultInstance;

        //         // Set a flag here to indicate whether Firebase is ready to use by your app.
        //     } else {
        //         UnityEngine.Debug.LogError(System.String.Format(
        //         "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
        //         // Firebase Unity SDK is not safe to use here.
        //     }
        // });


        // Debug.Log("a");
        // Firebase.Storage.FirebaseStorage storage = Firebase.Storage.FirebaseStorage.DefaultInstance;
        // Firebase.Storage.StorageReference storageReference = storage.GetReferenceFromUrl("gs://paintingproject-55bea.appspot.com");
        // Debug.Log("a");

        // storageReference.Child("Aliens1.png").GetBytesAsync(1024 * 1024).ContinueWith((System.Threading.Tasks.Task<byte[]> task) =>
        // {
        //     Debug.Log("b");
        //     if (task.IsFaulted || task.IsCanceled)
        //     {
        //         Debug.Log("c");
        //         Debug.Log(task.Exception.ToString());
        //     }
        //     else
        //     {
        //         Debug.Log("s");
        //         byte[] fileContents = task.Result;
        //         Texture2D texture = new Texture2D(1, 1);
        //         texture.LoadImage(fileContents);
        //         //if you need sprite for SpriteRenderer or Image
        //         Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f,texture.width, 
        //         texture.height), new Vector2(0.5f, 0.5f), 100.0f);
        //         Debug.Log("Finished downloading!");
        //     }
        //     Debug.Log("e");
        // });

        // Debug.Log("g");


        // FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        //  var storage = FirebaseStorage.GetInstance("gs://decent-tracer-842.appspot.com");
        FirebaseStorage storage = FirebaseStorage.GetInstance("gs://decent-tracer-842.appspot.com");
        StorageReference reference = storage.GetReferenceFromUrl("gs://decent-tracer-842.appspot.com/aliens/Thumbs/Aliens1.png");


        // Get a reference to the storage service, using the default Firebase App
        

        // Create a storage reference from our storage service
        // StorageReference storageRef = storage.GetReferenceFromUrl("gs://decent-tracer-842.appspot.com");
        // Firebase.Storage.StorageReference storage_ref = storage.GetReferenceFromUrl("gs://paintingproject-55bea.appspot.com");
        // Debug.Log(storage_ref);
        // Firebase.Storage.StorageReference storage_refURL = Firebase.Storage.FirebaseStorage.DefaultInstance.GetReferenceFromUrl("gs://decent-tracer-842.appspot.com/aliens/Thumbs/Aliens1.png");
        // Debug.Log(storage_refURL);

        // Get a reference to the storage service, using the default Firebase App
        // FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        // Debug.Log(storage);
        // var storage = FirebaseStorage.GetInstance("gs://decent-tracer-842.appspot.com");


        reference.GetDownloadUrlAsync().ContinueWithOnMainThread(task => {
            if (!task.IsFaulted && !task.IsCanceled) {
                Debug.Log("Download URL: " + task.Result);
                // ... now download the file via WWW or UnityWebRequest.
            }
        });

        const long maxAllowedSize = 1 * 1024 * 1024;
        // // storage_refURL.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task => {
        // //     if (task.IsFaulted || task.IsCanceled) {
        // //         Debug.LogException(task.Exception);
        // //         // Uh-oh, an error occurred!
        // //     }
        // //     else {
        // //         byte[] fileContents = task.Result;
        // //         Debug.Log("Finished downloading!");
        // //     }
        // // });


        reference.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogException(task.Exception);
                // Uh-oh, an error occurred!
            }
            else {
                byte[] fileContents = task.Result;
                Debug.Log("1Finished downloading!");
            }
        });
        // storage_ref.Child("Aliens1.png").GetBytesAsync(1024 * 1024)
        //     .ContinueWith((System.Threading.Tasks.Task<byte[]> task) =>
        //         {
        //             Debug.Log("2");
        //             if (task.IsFaulted || task.IsCanceled)
        //             {
        //                 Debug.Log(task.Exception.ToString());
        //             }
        //             else
        //             {
        //             byte[] fileContents = task.Result;
        //             Debug.Log("Load Image after getting result!");

        //             // Texture2D texture = new Texture2D(1024, 1024);
        //             // texture.LoadImage(fileContents);
        //             // testMaterial.SetTexture("_MainTex", texture);
        //             // plane.GetComponent<Renderer>().sharedMaterial = testMaterial;
        //             Debug.Log("finished");
        //             }
        //         });
            Debug.Log("3");
    }
}