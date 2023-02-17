using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using Firebase;
using Firebase.Extensions;
using Firebase.Storage;

#if UNITY_WEBGL
using System.IO;
#endif

public class ColoringBookManager : MonoBehaviour
{
    #region variables

    public Material maskTexMaterial;
    private Texture2D maskTex;
    public List<Sprite> maskTexList;
    public static int maskTexIndex = -1;
    public static string maskPath = "";
    public static string ID = "0";

    // list of drawmodes
    public enum DrawMode
    {
        Pencil,
        Marker,
        PaintBucket,
        Sticker
    }

    //	*** Default settings ***
    private Color32 paintColor = new Color32(255, 0, 0, 255);
    private int brushSize = 8; // default brush size
    private DrawMode drawMode = DrawMode.Pencil;
    private bool useLockArea = true;
    private byte[] lockMaskPixels; // locking mask pixels

    // Stickers
    public Texture2D[] stickers;
    private int selectedSticker = 0; // currently selected sticker index
    private byte[] stickerBytes;
    private int stickerWidth;
    private int stickerHeight;
    private int stickerWidthHalf;
    private int texWidthMinusStickerWidth;
    private int texHeightMinusStickerHeight;

    // UNDO
    private List<byte[]> undoPixels; // undo buffer(s)
    private int redoIndex = 0;
    private int RedoIndex
    {
        set
        {
            redoIndex = value;

            UndoRedoButtons[0].image.sprite = UndoRedoButtons[0].sprites[undoPixels.Count - RedoIndex - 1 > 0 ? 0 : 1];
            UndoRedoButtons[0].image.raycastTarget = undoPixels.Count - RedoIndex - 1 > 0;

            UndoRedoButtons[1].image.sprite = UndoRedoButtons[1].sprites[undoPixels.Count > 0 && RedoIndex > 0 ? 0 : 1];
            UndoRedoButtons[1].image.raycastTarget = undoPixels.Count > 0 && RedoIndex > 0;
        }

        get
        {
            return redoIndex;
        }
    }

    //	*** private variables ***
    private byte[] pixels; // byte array for texture painting, this is the image that we paint into.
    private byte[] maskPixels; // byte array for mask texture
    private byte[] clearPixels; // byte array for clearing texture

    private Texture2D tex; // texture that we paint into (it gets updated from pixels[] array when painted)

    private int texWidth = 1024;
    private int texHeight = 1024;
    private RaycastHit hit;
    private bool wentOutside = false;

    private Vector2 pixelUV; // with mouse
    private Vector2 pixelUVOld; // with mouse

    private bool textureNeedsUpdate = false; // if we have modified texture

    ////////////////////////////////////////////////////

    [Space]
    public List<RectTransform> PanelColors; // 0.PencilPanel, 1.MarkerPanel, 2.PaintBucketPanel, 3.StickerPanel 
    private Vector3 panelStartPos = Vector3.zero, panelEndPos = Vector3.zero;

    public List<PaintingButton> drawModeButton; // 0.Pencil, 1.Marker, 2.PaintBucket, 3.Sticker
    [System.Serializable]
    public class PaintingButton
    {
        public string name;
        public Image image;
        public List<Sprite> sprites;
    }

    public List<PaintingButton> UndoRedoButtons; // 0.undo, 1.redo
    public PaintingButton brushSizeButton;
    public PaintingButton brushSizeButton1;
    public PaintingButton brushSizeButton2;
    public PaintingButton brushSizeButton3;
    public PaintingButton musicButtonController; // 0.Off, 1.On
    public PaintingButton buttonCamera; // 0.Disable

    private int changeThemeIndex = 0;
    private int ChangeThemeIndex
    {
        set
        {
            if (value >= themes.colors.Count)
            {
                value = 0;
            }

            changeThemeIndex = value;

            PlayerPrefs.SetInt("Theme", value);
            PlayerPrefs.Save();

            for (int i = 0; i < themes.spList.Count; i++)
            {
                themes.spList[i].color = themes.colors[value].color[i];
            }
        }

        get
        {
            return changeThemeIndex;
        }
    }

    public Themes themes;

    [System.Serializable]
    public class Themes
    {
        public List<Image> spList; // 0.bottom // 1.rightMenu1 // 2.rightMenu2
        public List<Colors> colors;

        [System.Serializable]
        public class Colors
        {
            public string name;
            public List<Color> color;
        }
    }

    public GameObject waterMark;

    #endregion


    #region Init And Control Functions

    private void Awake()
    {
        Camera.main.aspect = 16 / 9f;

        GetComponent<Renderer>().sortingOrder = -99;

        if (maskTexIndex < 0)
        {
            maskTex = null;
            InitializeEverything();
        }
        else
        {
            // maskTex = DuplicateTexture(maskTexList[maskTexIndex].texture);
            maskTex = DuplicateTexture(ScrollListManagerColoring.allTexturesDic[maskPath].texture);
            // GetFirebaseData();
        }
        InitializeEverything();
    }

    public async void GetFirebaseData () {

        FirebaseStorage storage = FirebaseStorage.GetInstance("gs://decent-tracer-842.appspot.com");

        Debug.Log(maskPath);
        StorageReference reference = storage.GetReferenceFromUrl(maskPath);

        const long maxAllowedSize = 1024 * 1024;
        
        await reference.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task => {
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
                Texture2D texture = new Texture2D(1024, 1024);
                texture.LoadImage(fileContents);

                maskTex = DuplicateTexture(texture);
                InitializeEverything();
            }
        });
    }

    private Texture2D DuplicateTexture(Texture2D source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }

    private void InitializeEverything()
    {
        CreateFullScreenQuad();

        // create texture
        if (maskTex)
        {
            GetComponent<Renderer>().material = maskTexMaterial;

            texWidth = maskTex.width;
            texHeight = maskTex.height;
            GetComponent<Renderer>().material.SetTexture("_MaskTex", maskTex);

            useLockArea = true;
        }
        else
        {
            texWidth = 1024;
            texHeight = 1024;

            useLockArea = false;
        }

        if (!GetComponent<Renderer>().material.HasProperty("_MainTex")) Debug.LogError("Fatal error: Current shader doesn't have a property: '_MainTex'");


        // create new texture
        tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        GetComponent<Renderer>().material.SetTexture("_MainTex", tex);

        // init pixels array
        pixels = new byte[texWidth * texHeight * 4];

        OnClearButtonClicked();

        // set texture modes
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        //tex.wrapMode = TextureWrapMode.Repeat;

        if (maskTex)
        {
            ReadMaskImage();
        }

        // undo system
        undoPixels = new List<byte[]>();
        undoPixels.Add(new byte[texWidth * texHeight * 4]);
        RedoIndex = 0;
        Debug.Log("texWidth " + texWidth);
        Debug.Log("texHeight " + texHeight);
        byte[] loadPixels = new byte[texWidth * texHeight * 4];
        loadPixels = LoadImage(ID);
        if (loadPixels != null)
        {
            pixels = loadPixels;
            Debug.Log(loadPixels.Length);
            Debug.Log(undoPixels[0].Length);
            System.Array.Copy(pixels, undoPixels[0], pixels.Length);
            tex.LoadRawTextureData(pixels);
            tex.Apply(false);
            // System.Array.Copy(pixels, undoPixels[0], pixels.Length);
        }
        else
        {
            System.Array.Copy(pixels, undoPixels[0], pixels.Length);
        }

        System.Array.Copy(pixels, undoPixels[0], pixels.Length);

        // locking mask enabled
        if (useLockArea)
        {
            lockMaskPixels = new byte[texWidth * texHeight * 4];
        }
    }

    private void CreateFullScreenQuad()
    {
        Camera cam = Camera.main;
        // create mesh plane, fits in camera view (with screensize adjust taken into consideration)
        Mesh go_Mesh = GetComponent<MeshFilter>().mesh;
        go_Mesh.Clear();
        go_Mesh.vertices = new[] {
                cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane + 0.1f)), // bottom left
				cam.ScreenToWorldPoint(new Vector3(0, cam.pixelHeight, cam.nearClipPlane + 0.1f)), // top left
				cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, cam.nearClipPlane + 0.1f)), // top right
				cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, 0, cam.nearClipPlane + 0.1f)) // bottom right
			};
        go_Mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        go_Mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

        go_Mesh.RecalculateNormals();

        go_Mesh.tangents = new[] { new Vector4(1.0f, 0.0f, 0.0f, -1.0f), new Vector4(1.0f, 0.0f, 0.0f, -1.0f), new Vector4(1.0f, 0.0f, 0.0f, -1.0f), new Vector4(1.0f, 0.0f, 0.0f, -1.0f) };

        // add mesh collider
        // gameObject.AddComponent<MeshCollider>();
        gameObject.GetComponent<MeshCollider>().sharedMesh = go_Mesh;
    }

    private void ReadMaskImage()
    {
        maskPixels = new byte[texWidth * texHeight * 4];

        int pixel = 0;
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                Color c = maskTex.GetPixel(x, y);
                maskPixels[pixel] = (byte)(c.r * 255);
                maskPixels[pixel + 1] = (byte)(c.g * 255);
                maskPixels[pixel + 2] = (byte)(c.b * 255);
                maskPixels[pixel + 3] = (byte)(c.a * 255);
                pixel += 4;
            }
        }
    }

    private byte[] LoadImage(string key)
    {
#if UNITY_WEBGL
        string file = Application.persistentDataPath + "/Landscape" + key + ".sav";
        if (File.Exists(file))
        {
            return System.Convert.FromBase64String(File.ReadAllText(file));
        }
        else
        {
            return null;
        }
#else
        if (PlayerPrefs.HasKey(key))
        {
            return System.Convert.FromBase64String(PlayerPrefs.GetString(key));
        }
        else
        {
            return null;
        }
#endif
    }

    private void SaveImage(string key)
    {
#if UNITY_WEBGL
        string file = Application.persistentDataPath + "/Landscape" + key + ".sav";
        string fileData = System.Convert.ToBase64String(pixels);
        File.WriteAllText(file, fileData);
#else
        PlayerPrefs.SetString(key, System.Convert.ToBase64String(pixels));
        PlayerPrefs.Save();
#endif
    }

    private void Start()
    {
#if UNITY_ANDROID
        if (JavadRastadAndroidRuntimePermissions.CheckDeniedStoragePermissions())
        {
            buttonCamera.image.sprite = buttonCamera.sprites[0];
            buttonCamera.image.raycastTarget = false;
        }
#endif
        SetPanelsUIScale((int)DrawMode.Pencil);

        OnDrawModeButtonClicked((int)DrawMode.Pencil);

        OnBrushButtonClicked(PanelColors[(int)drawMode].GetChild(0).GetComponent<ButtonScript>());

        OnChangeBrushSizeButtonClicked1();

        OnStickerButtonClicked(PanelColors[(int)DrawMode.Sticker].GetChild(0).GetComponent<ButtonScript>());

        LoadSetting();
    }

    private void SetPanelsUIScale(int current)
    {
        float h = themes.spList[3].rectTransform.rect.height;

        foreach (RectTransform panel in PanelColors)
        {
            panel.offsetMax = new Vector2(150, -h * 2);
            panel.offsetMin = new Vector2(150, -h * 3);
        }

        panelEndPos = PanelColors[current].localPosition;
        panelStartPos = panelEndPos;
        panelStartPos.y += (h * 2);

        PanelColors[current].localPosition = panelStartPos;
    }

    private void LoadSetting()
    {
        // Music
        musicButtonController.image.sprite = musicButtonController.sprites[(int)AudioListener.volume];

        // Theme
        ChangeThemeIndex = PlayerPrefs.GetInt("Theme", 0);
    }

    private void LateUpdate()
    {
        MousePaint();

        UpdateTexture();
    }

    private void MousePaint()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider == null || !hit.collider.gameObject.name.Contains("PaintingBoard"))
                {
                    return;
                }
            }
            else
            {
                RaycastHit2D hit2 = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

                if (hit2.collider == null || !hit2.collider.gameObject.name.Contains("PaintingBoard"))
                {
                    return;
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (useLockArea)
            {
                if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, 1)) return;
                CreateAreaLockMask((int)(hit.textureCoord.x * texWidth), (int)(hit.textureCoord.y * texHeight));
            }

            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, 1)) { wentOutside = true; return; }

            pixelUVOld = pixelUV; // take previous value, so can compare them
            pixelUV = hit.textureCoord;
            pixelUV.x *= texWidth;
            pixelUV.y *= texHeight;

            if (wentOutside) { pixelUVOld = pixelUV; wentOutside = false; }

            // lets paint where we hit
            switch (drawMode)
            {
                case DrawMode.Sticker: // Sticker
                    DrawSticker((int)pixelUV.x, (int)pixelUV.y);
                    break;

                default: // unknown mode
                    break;
            }

            textureNeedsUpdate = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, 1)) { wentOutside = true; return; }

            // when starting, grab undo buffer first
            if (RedoIndex > 0)
            {
                undoPixels.RemoveRange(undoPixels.Count - RedoIndex, RedoIndex);
            }

            undoPixels.Add(new byte[texWidth * texHeight * 4]);
            System.Array.Copy(pixels, undoPixels[undoPixels.Count - 1], pixels.Length);

            RedoIndex = 0;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            // Only if we hit something, then we continue
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, 1)) { wentOutside = true; return; }

            pixelUVOld = pixelUV; // take previous value, so can compare them
            pixelUV = hit.textureCoord;
            pixelUV.x *= texWidth;
            pixelUV.y *= texHeight;

            if (wentOutside) { pixelUVOld = pixelUV; wentOutside = false; }

            // lets paint where we hit
            switch (drawMode)
            {
                case DrawMode.Pencil: // drawing
                    DrawCircle((int)pixelUV.x, (int)pixelUV.y);
                    break;

                case DrawMode.Marker: // drawing
                    DrawAdditiveCircle((int)pixelUV.x, (int)pixelUV.y);
                    break;

                //case DrawMode.Sticker: // Sticker
                //    DrawSticker((int)pixelUV.x, (int)pixelUV.y);
                //    break;

                case DrawMode.PaintBucket: // floodfill
                    if (maskTex)
                    {
                        FloodFillMaskOnlyWithThreshold((int)pixelUV.x, (int)pixelUV.y);
                    }
                    else
                    {
                        FloodFillWithTreshold((int)pixelUV.x, (int)pixelUV.y);
                    }
                    break;

                default: // unknown mode
                    break;
            }

            textureNeedsUpdate = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // take this position as start position
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, 1)) return;

            pixelUVOld = pixelUV;
        }

        // check distance from previous drawing point and connect them with DrawLine
        if (Vector2.Distance(pixelUV, pixelUVOld) > brushSize)
        {
            switch (drawMode)
            {
                case DrawMode.Pencil: // drawing
                    DrawLine(pixelUVOld, pixelUV);
                    break;

                case DrawMode.Marker: // drawing
                    DrawAdditiveLine(pixelUVOld, pixelUV);
                    break;

                //case DrawMode.Sticker:f
                //    DrawLineWithSticker(pixelUVOld, pixelUV);
                //    break;

                default: // other modes
                    break;
            }
            pixelUVOld = pixelUV;
            textureNeedsUpdate = true;
        }
    }

    private void CreateAreaLockMask(int x, int y)
    {
        if (maskTex)
        {
            LockAreaFillWithThresholdMaskOnly(x, y);
        }
        else
        {
            LockMaskFillWithThreshold(x, y);
        }
    }

    private void LockAreaFillWithThresholdMaskOnly(int x, int y)
    {
        // create locking mask floodfill, using threshold, checking pixels from mask only

        // get canvas color from this point
        byte hitColorR = maskPixels[((texWidth * (y) + x) * 4) + 0];
        byte hitColorG = maskPixels[((texWidth * (y) + x) * 4) + 1];
        byte hitColorB = maskPixels[((texWidth * (y) + x) * 4) + 2];
        byte hitColorA = maskPixels[((texWidth * (y) + x) * 4) + 3];

        Queue<int> fillPointX = new Queue<int>();
        Queue<int> fillPointY = new Queue<int>();
        fillPointX.Enqueue(x);
        fillPointY.Enqueue(y);

        int ptsx, ptsy;
        int pixel = 0;

        lockMaskPixels = new byte[texWidth * texHeight * 4];

        while (fillPointX.Count > 0)
        {

            ptsx = fillPointX.Dequeue();
            ptsy = fillPointY.Dequeue();

            if (ptsy - 1 > -1)
            {
                pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down

                if (lockMaskPixels[pixel] == 0 // this pixel is not used yet
                    && (CompareThreshold(maskPixels[pixel + 0], hitColorR)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                    && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                    && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy - 1);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx + 1 < texWidth)
            {
                pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                if (lockMaskPixels[pixel] == 0
                    && (CompareThreshold(maskPixels[pixel + 0], hitColorR)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                    && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                    && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                {
                    fillPointX.Enqueue(ptsx + 1);
                    fillPointY.Enqueue(ptsy);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx - 1 > -1)
            {
                pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                if (lockMaskPixels[pixel] == 0
                    && (CompareThreshold(maskPixels[pixel + 0], hitColorR)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                    && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                    && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                {
                    fillPointX.Enqueue(ptsx - 1);
                    fillPointY.Enqueue(ptsy);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsy + 1 < texHeight)
            {
                pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                if (lockMaskPixels[pixel] == 0
                    && (CompareThreshold(maskPixels[pixel + 0], hitColorR)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(maskPixels[pixel + 1], hitColorG))
                    && (CompareThreshold(maskPixels[pixel + 2], hitColorB))
                    && (CompareThreshold(maskPixels[pixel + 3], hitColorA)))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy + 1);
                    lockMaskPixels[pixel] = 1;
                }
            }
        }
    }

    private void LockMaskFillWithThreshold(int x, int y)
    {
        // create locking mask floodfill, using threshold

        // get canvas color from this point
        byte hitColorR = pixels[((texWidth * (y) + x) * 4) + 0];
        byte hitColorG = pixels[((texWidth * (y) + x) * 4) + 1];
        byte hitColorB = pixels[((texWidth * (y) + x) * 4) + 2];
        byte hitColorA = pixels[((texWidth * (y) + x) * 4) + 3];

        Queue<int> fillPointX = new Queue<int>();
        Queue<int> fillPointY = new Queue<int>();
        fillPointX.Enqueue(x);
        fillPointY.Enqueue(y);

        int ptsx, ptsy;
        int pixel = 0;

        lockMaskPixels = new byte[texWidth * texHeight * 4];

        while (fillPointX.Count > 0)
        {

            ptsx = fillPointX.Dequeue();
            ptsy = fillPointY.Dequeue();

            if (ptsy - 1 > -1)
            {
                pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down

                if (lockMaskPixels[pixel] == 0 // this pixel is not used yet
                    && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                    && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                    && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy - 1);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx + 1 < texWidth)
            {
                pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                if (lockMaskPixels[pixel] == 0
                    && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                    && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                    && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                {
                    fillPointX.Enqueue(ptsx + 1);
                    fillPointY.Enqueue(ptsy);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx - 1 > -1)
            {
                pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                if (lockMaskPixels[pixel] == 0
                    && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                    && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                    && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                {
                    fillPointX.Enqueue(ptsx - 1);
                    fillPointY.Enqueue(ptsy);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsy + 1 < texHeight)
            {
                pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                if (lockMaskPixels[pixel] == 0
                    && (CompareThreshold(pixels[pixel + 0], hitColorR) || CompareThreshold(pixels[pixel + 0], paintColor.r)) // if pixel is same as hit color OR same as paint color
                    && (CompareThreshold(pixels[pixel + 1], hitColorG) || CompareThreshold(pixels[pixel + 1], paintColor.g))
                    && (CompareThreshold(pixels[pixel + 2], hitColorB) || CompareThreshold(pixels[pixel + 2], paintColor.b))
                    && (CompareThreshold(pixels[pixel + 3], hitColorA) || CompareThreshold(pixels[pixel + 3], paintColor.a)))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy + 1);
                    lockMaskPixels[pixel] = 1;
                }
            }
        }
    }

    private void UpdateTexture()
    {
        if (textureNeedsUpdate)
        {
            textureNeedsUpdate = false;
            tex.LoadRawTextureData(pixels);
            tex.Apply(false);
        }
    }

    #endregion


    #region OnButtonsClicked

    public void OnDrawModeButtonClicked(int drawModeIndex)
    {
        foreach (PaintingButton button in drawModeButton)
        {
            button.image.sprite = button.sprites[1];
        }

        drawModeButton[drawModeIndex].image.sprite = drawModeButton[drawModeIndex].sprites[0];

        int currentDrawMode = (int)drawMode;

        if (currentDrawMode == drawModeIndex)
            return;

        SetPanelsUIScale(currentDrawMode);

        PanelColors[currentDrawMode].GetComponent<ButtonScript>().StartMyMoveAction(PanelColors[currentDrawMode].localPosition, panelEndPos, 0.5f);

        PanelColors[drawModeIndex].GetComponent<ButtonScript>().StartMyMoveAction(PanelColors[drawModeIndex].localPosition, panelStartPos, 0.5f);

        drawMode = (DrawMode)drawModeIndex;
    }

    public void OnBrushButtonClicked(ButtonScript sender)
    {
        paintColor = sender.GetComponent<Image>().color;
        brushSizeButton.image.color = paintColor; // set current color image
        brushSizeButton1.image.color = paintColor;
        brushSizeButton2.image.color = paintColor;
        brushSizeButton3.image.color = paintColor;

        switch (drawMode)
        {
            case DrawMode.Pencil:
            case DrawMode.Marker:
            case DrawMode.PaintBucket:

                int selectedNumber = sender.transform.GetSiblingIndex();

                for (int i = 0; i < PanelColors[(int)DrawMode.Pencil].childCount; i++)
                {
                    Vector2 min = PanelColors[(int)DrawMode.Pencil].GetChild(i).GetComponent<RectTransform>().anchorMin;
                    Vector2 max = PanelColors[(int)DrawMode.Pencil].GetChild(i).GetComponent<RectTransform>().anchorMax;

                    if (i == selectedNumber)
                    {
                        min.y = 0.34f;
                        max.y = 1f;
                    }
                    else
                    {
                        min.y = 0.22f;
                        max.y = 0.88f;
                    }

                    PanelColors[(int)DrawMode.Pencil].GetChild(i).GetComponent<RectTransform>().anchorMin = min;
                    PanelColors[(int)DrawMode.Pencil].GetChild(i).GetComponent<RectTransform>().anchorMax = max;

                    ////////////////////////////////////////

                    min = PanelColors[(int)DrawMode.Marker].GetChild(i).GetComponent<RectTransform>().anchorMin;
                    max = PanelColors[(int)DrawMode.Marker].GetChild(i).GetComponent<RectTransform>().anchorMax;

                    if (i == selectedNumber)
                    {
                        min.y = 0.34f;
                        max.y = 1f;
                    }
                    else
                    {
                        min.y = 0.22f;
                        max.y = 0.88f;
                    }

                    PanelColors[(int)DrawMode.Marker].GetChild(i).GetComponent<RectTransform>().anchorMin = min;
                    PanelColors[(int)DrawMode.Marker].GetChild(i).GetComponent<RectTransform>().anchorMax = max;
                }

                for (int i = 0; i < PanelColors[(int)DrawMode.PaintBucket].childCount; i++)
                {
                    PanelColors[(int)DrawMode.PaintBucket].GetChild(i).GetChild(0).gameObject.SetActive(false);
                }

                PanelColors[(int)DrawMode.PaintBucket].GetChild(selectedNumber).GetChild(0).gameObject.SetActive(true);
                break;
        }
    }

    public void OnStickerButtonClicked(ButtonScript sender)
    {
        selectedSticker = sender.transform.GetSiblingIndex();

        for (int i = 0; i < PanelColors[(int)DrawMode.Sticker].childCount; i++)
        {
            PanelColors[(int)DrawMode.Sticker].GetChild(i).GetChild(0).gameObject.SetActive(false);
        }

        PanelColors[(int)DrawMode.Sticker].GetChild(selectedSticker).GetChild(0).gameObject.SetActive(true);

        // tell mobile paint to read sticker pixel data
        stickerWidth = stickers[selectedSticker].width;
        stickerHeight = stickers[selectedSticker].height;
        stickerBytes = new byte[stickerWidth * stickerHeight * 4];

        int pixel = 0;
        for (int y = 0; y < stickerHeight; y++)
        {
            for (int x = 0; x < stickerWidth; x++)
            {
                Color brushPixel = stickers[selectedSticker].GetPixel(x, y);
                stickerBytes[pixel] = (byte)(brushPixel.r * 255);
                stickerBytes[pixel + 1] = (byte)(brushPixel.g * 255);
                stickerBytes[pixel + 2] = (byte)(brushPixel.b * 255);
                stickerBytes[pixel + 3] = (byte)(brushPixel.a * 255);
                pixel += 4;
            }
        }

        // precalculate values
        stickerWidthHalf = (int)(stickerWidth * 0.5f);
        texWidthMinusStickerWidth = texWidth - stickerWidth;
        texHeightMinusStickerHeight = texHeight - stickerHeight;
    }

    public void OnChangeBrushSizeButtonClicked1()
    {
        brushSize = 8;

        brushSizeButton.image.sprite = brushSizeButton.sprites[0];
        brushSizeButton1.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton2.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton3.image.sprite = brushSizeButton.sprites[1];
    }

    public void OnChangeBrushSizeButtonClicked2()
    {
        brushSize = 14;

        brushSizeButton.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton1.image.sprite = brushSizeButton.sprites[0];
        brushSizeButton2.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton3.image.sprite = brushSizeButton.sprites[1];
    }
    
    public void OnChangeBrushSizeButtonClicked3()
    {
        brushSize = 20;

        brushSizeButton.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton1.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton2.image.sprite = brushSizeButton.sprites[0];
        brushSizeButton3.image.sprite = brushSizeButton.sprites[1];
    }

    public void OnChangeBrushSizeButtonClicked4()
    {
        brushSize = 26;

        brushSizeButton.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton1.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton2.image.sprite = brushSizeButton.sprites[1];
        brushSizeButton3.image.sprite = brushSizeButton.sprites[0];
    }

    public void OnUndoButtonClicked()
    {
        if (undoPixels.Count - RedoIndex - 1 > 0)
        {
            System.Array.Copy(undoPixels[undoPixels.Count - RedoIndex - 2], pixels, undoPixels[undoPixels.Count - RedoIndex - 2].Length);
            tex.LoadRawTextureData(undoPixels[undoPixels.Count - RedoIndex - 2]);
            tex.Apply(false);

            RedoIndex++;
        }
    }

    public void OnRedoButtonClicked()
    {
        if (undoPixels.Count > 0 && RedoIndex > 0)
        {
            System.Array.Copy(undoPixels[undoPixels.Count - RedoIndex], pixels, undoPixels[undoPixels.Count - RedoIndex].Length);
            tex.LoadRawTextureData(undoPixels[undoPixels.Count - RedoIndex]);
            tex.Apply(false);

            RedoIndex--;
        }
    }

    public void OnClearButtonClicked()
    {
        int pixel = 0;
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                pixels[pixel] = 255;
                pixels[pixel + 1] = 255;
                pixels[pixel + 2] = 255;
                pixels[pixel + 3] = 255;
                pixel += 4;
            }
        }
        tex.LoadRawTextureData(pixels);
        tex.Apply(false);

        if (undoPixels != null)
        {
            if (RedoIndex > 0)
            {
                undoPixels.RemoveRange(undoPixels.Count - RedoIndex, RedoIndex);
                RedoIndex = 0;
            }

            undoPixels.Add(new byte[texWidth * texHeight * 4]);
            System.Array.Copy(pixels, undoPixels[undoPixels.Count - 1], pixels.Length);
        }
    }

    public void OnScreenshotButtonClicked()
    {
        StartCoroutine(OnSavePictureClickListener());
    }

    private IEnumerator OnSavePictureClickListener()
    {
#if UNITY_ANDROID
        if (JavadRastadAndroidRuntimePermissions.RequestStoragePermissions())
        {
#endif
        MusicController.USE.PlaySound(MusicController.USE.cameraSound);

        waterMark.SetActive(true);
        StartCoroutine(ScreenshotManager.SaveForPaint("MyPicture", "ColoringBook"));
        yield return new WaitForSeconds(1f);
        waterMark.SetActive(false);
#if UNITY_ANDROID
        }
        else
        {
            buttonCamera.image.sprite = buttonCamera.sprites[0];
            buttonCamera.image.raycastTarget = false;
        }
#endif

        yield return null;
    }

    public void OnMusicControllerButtonClicked()
    {
        MusicController.USE.ChangeMusicSetting();

        musicButtonController.image.sprite = musicButtonController.sprites[(int)AudioListener.volume];
    }

    public void OnChangeThemeButtonClicked()
    {
        ChangeThemeIndex++;
    }

    public void OnHomeButtonClicked()
    {
        SaveImage(ID);

        SceneManager.LoadScene("MainScene");
    }

    #endregion


    #region Painting Functions

    private void DrawCircle(int x, int y)
    {
        int pixel = 0;

        // draw fast circle: 
        int r2 = brushSize * brushSize;
        int area = r2 << 2;
        int rr = brushSize << 1;
        for (int i = 0; i < area; i++)
        {
            int tx = (i % rr) - brushSize;
            int ty = (i / rr) - brushSize;
            if (tx * tx + ty * ty < r2)
            {
                if (x + tx < 0 || y + ty < 0 || x + tx >= texWidth || y + ty >= texHeight) continue;

                pixel = (texWidth * (y + ty) + x + tx) * 4;

                if (!useLockArea || (useLockArea && lockMaskPixels[pixel] == 1))
                {
                    pixels[pixel] = paintColor.r;
                    pixels[pixel + 1] = paintColor.g;
                    pixels[pixel + 2] = paintColor.b;
                    pixels[pixel + 3] = paintColor.a;
                }

            }
        }
    }

    private void DrawAdditiveCircle(int x, int y)
    {
        int pixel = 0;

        // draw fast circle: 
        int r2 = brushSize * brushSize;
        int area = r2 << 2;
        int rr = brushSize << 1;
        for (int i = 0; i < area; i++)
        {
            int tx = (i % rr) - brushSize;
            int ty = (i / rr) - brushSize;
            if (tx * tx + ty * ty < r2)
            {
                if (x + tx < 0 || y + ty < 0 || x + tx >= texWidth || y + ty >= texHeight) continue;

                pixel = (texWidth * (y + ty) + x + tx) * 4;

                // additive over white also
                if (!useLockArea || (useLockArea && lockMaskPixels[pixel] == 1))
                {
                    pixels[pixel] = (byte)Mathf.Lerp(pixels[pixel], paintColor.r, paintColor.a / 255f * 0.1f);
                    pixels[pixel + 1] = (byte)Mathf.Lerp(pixels[pixel + 1], paintColor.g, paintColor.a / 255f * 0.1f);
                    pixels[pixel + 2] = (byte)Mathf.Lerp(pixels[pixel + 2], paintColor.b, paintColor.a / 255f * 0.1f);
                    pixels[pixel + 3] = (byte)Mathf.Lerp(pixels[pixel + 3], paintColor.a, paintColor.a / 255 * 0.1f);
                }

            }
        }
    }

    private void DrawSticker(int px, int py)
    {
        // get position where we paint
        int startX = (int)(px - stickerWidthHalf);
        int startY = (int)(py - stickerWidthHalf);

        if (startX < 0)
        {
            startX = 0;
        }
        else {
            if (startX + stickerWidth >= texWidth) startX = texWidthMinusStickerWidth;
        }

        if (startY < 1)
        {
            startY = 1;
        }
        else {
            if (startY + stickerHeight >= texHeight) startY = texHeightMinusStickerHeight;
        }


        int pixel = (texWidth * startY + startX) * 4;
        int brushPixel = 0;

        for (int y = 0; y < stickerHeight; y++)
        {
            for (int x = 0; x < stickerWidth; x++)
            {
                brushPixel = (stickerWidth * (y) + x) * 4;

                // brush alpha is over 0 in this pixel
                if (stickerBytes[brushPixel + 3] > 0)
                {
                    pixels[pixel] = stickerBytes[brushPixel];
                    pixels[pixel + 1] = stickerBytes[brushPixel + 1];
                    pixels[pixel + 2] = stickerBytes[brushPixel + 2];
                    pixels[pixel + 3] = stickerBytes[brushPixel + 3];
                }

                pixel += 4;

            } // for x

            pixel = (texWidth * (startY == 0 ? 1 : startY + y) + startX + 1) * 4;
        } // for y
    }

    private void FloodFillMaskOnlyWithThreshold(int x, int y)
    {
        // get canvas hit color
        byte hitColorR = maskPixels[((texWidth * (y) + x) * 4) + 0];
        byte hitColorG = maskPixels[((texWidth * (y) + x) * 4) + 1];
        byte hitColorB = maskPixels[((texWidth * (y) + x) * 4) + 2];
        byte hitColorA = maskPixels[((texWidth * (y) + x) * 4) + 3];

        if (paintColor.r == hitColorR && paintColor.g == hitColorG && paintColor.b == hitColorB && paintColor.a == hitColorA) return;

        Queue<int> fillPointX = new Queue<int>();
        Queue<int> fillPointY = new Queue<int>();
        fillPointX.Enqueue(x);
        fillPointY.Enqueue(y);

        int ptsx, ptsy;
        int pixel = 0;

        lockMaskPixels = new byte[texWidth * texHeight * 4];

        while (fillPointX.Count > 0)
        {
            ptsx = fillPointX.Dequeue();
            ptsy = fillPointY.Dequeue();

            if (ptsy - 1 > -1)
            {
                pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                    && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                    && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                    && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy - 1);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx + 1 < texWidth)
            {
                pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                    && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                    && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                    && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx + 1);
                    fillPointY.Enqueue(ptsy);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx - 1 > -1)
            {
                pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                    && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                    && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                    && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx - 1);
                    fillPointY.Enqueue(ptsy);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsy + 1 < texHeight)
            {
                pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(maskPixels[pixel + 0], hitColorR)
                    && CompareThreshold(maskPixels[pixel + 1], hitColorG)
                    && CompareThreshold(maskPixels[pixel + 2], hitColorB)
                    && CompareThreshold(maskPixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy + 1);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }
        }
    }

    private void FloodFillWithTreshold(int x, int y)
    {
        // get canvas hit color
        byte hitColorR = pixels[((texWidth * (y) + x) * 4) + 0];
        byte hitColorG = pixels[((texWidth * (y) + x) * 4) + 1];
        byte hitColorB = pixels[((texWidth * (y) + x) * 4) + 2];
        byte hitColorA = pixels[((texWidth * (y) + x) * 4) + 3];

        if (paintColor.r == hitColorR && paintColor.g == hitColorG && paintColor.b == hitColorB && paintColor.a == hitColorA) return;

        Queue<int> fillPointX = new Queue<int>();
        Queue<int> fillPointY = new Queue<int>();
        fillPointX.Enqueue(x);
        fillPointY.Enqueue(y);

        int ptsx, ptsy;
        int pixel = 0;

        lockMaskPixels = new byte[texWidth * texHeight * 4];

        while (fillPointX.Count > 0)
        {

            ptsx = fillPointX.Dequeue();
            ptsy = fillPointY.Dequeue();

            if (ptsy - 1 > -1)
            {
                pixel = (texWidth * (ptsy - 1) + ptsx) * 4; // down
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(pixels[pixel + 0], hitColorR)
                    && CompareThreshold(pixels[pixel + 1], hitColorG)
                    && CompareThreshold(pixels[pixel + 2], hitColorB)
                    && CompareThreshold(pixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy - 1);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx + 1 < texWidth)
            {
                pixel = (texWidth * ptsy + ptsx + 1) * 4; // right
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(pixels[pixel + 0], hitColorR)
                    && CompareThreshold(pixels[pixel + 1], hitColorG)
                    && CompareThreshold(pixels[pixel + 2], hitColorB)
                    && CompareThreshold(pixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx + 1);
                    fillPointY.Enqueue(ptsy);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsx - 1 > -1)
            {
                pixel = (texWidth * ptsy + ptsx - 1) * 4; // left
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(pixels[pixel + 0], hitColorR)
                    && CompareThreshold(pixels[pixel + 1], hitColorG)
                    && CompareThreshold(pixels[pixel + 2], hitColorB)
                    && CompareThreshold(pixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx - 1);
                    fillPointY.Enqueue(ptsy);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }

            if (ptsy + 1 < texHeight)
            {
                pixel = (texWidth * (ptsy + 1) + ptsx) * 4; // up
                if (lockMaskPixels[pixel] == 0
                    && CompareThreshold(pixels[pixel + 0], hitColorR)
                    && CompareThreshold(pixels[pixel + 1], hitColorG)
                    && CompareThreshold(pixels[pixel + 2], hitColorB)
                    && CompareThreshold(pixels[pixel + 3], hitColorA))
                {
                    fillPointX.Enqueue(ptsx);
                    fillPointY.Enqueue(ptsy + 1);
                    DrawPoint(pixel);
                    lockMaskPixels[pixel] = 1;
                }
            }
        }
    }

    private bool CompareThreshold(byte a, byte b)
    {
        if (a < b)
        {
            a ^= b; b ^= a; a ^= b;
        }

        return (a - b) <= 128;
    }

    private void DrawPoint(int pixel)
    {
        pixels[pixel] = paintColor.r;
        pixels[pixel + 1] = paintColor.g;
        pixels[pixel + 2] = paintColor.b;
        pixels[pixel + 3] = paintColor.a;
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx, sy;
        if (x0 < x1) { sx = 1; } else { sx = -1; }
        if (y0 < y1) { sy = 1; } else { sy = -1; }
        int err = dx - dy;
        bool loop = true;
        int minDistance = (int)(brushSize >> 1);
        int pixelCount = 0;
        int e2;
        while (loop)
        {
            pixelCount++;
            if (pixelCount > minDistance)
            {
                pixelCount = 0;
                DrawCircle(x0, y0);
            }
            if ((x0 == x1) && (y0 == y1)) loop = false;
            e2 = 2 * err;
            if (e2 > -dy)
            {
                err = err - dy;
                x0 = x0 + sx;
            }
            if (e2 < dx)
            {
                err = err + dx;
                y0 = y0 + sy;
            }
        }
    }

    private void DrawAdditiveLine(Vector2 start, Vector2 end)
    {
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx, sy;
        if (x0 < x1) { sx = 1; } else { sx = -1; }
        if (y0 < y1) { sy = 1; } else { sy = -1; }
        int err = dx - dy;
        bool loop = true;
        int minDistance = (int)(brushSize >> 1);
        int pixelCount = 0;
        int e2;
        while (loop)
        {
            pixelCount++;
            if (pixelCount > minDistance)
            {
                pixelCount = 0;
                DrawAdditiveCircle(x0, y0);
            }
            if ((x0 == x1) && (y0 == y1)) loop = false;
            e2 = 2 * err;
            if (e2 > -dy)
            {
                err = err - dy;
                x0 = x0 + sx;
            }
            if (e2 < dx)
            {
                err = err + dx;
                y0 = y0 + sy;
            }
        }
    }

    private void DrawLineWithSticker(Vector2 start, Vector2 end)
    {
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx, sy;
        if (x0 < x1) { sx = 1; } else { sx = -1; }
        if (y0 < y1) { sy = 1; } else { sy = -1; }
        int err = dx - dy;
        bool loop = true;
        //			int minDistance=brushSize-1;
        int minDistance = (int)(brushSize >> 1); // divide by 2, you might want to set mindistance to smaller value, to avoid gaps between brushes when moving fast
        int pixelCount = 0;
        int e2;
        while (loop)
        {
            pixelCount++;
            if (pixelCount > minDistance)
            {
                pixelCount = 0;
                DrawSticker(x0, y0);
            }
            if ((x0 == x1) && (y0 == y1)) loop = false;
            e2 = 2 * err;
            if (e2 > -dy)
            {
                err = err - dy;
                x0 = x0 + sx;
            }
            if (e2 < dx)
            {
                err = err + dx;
                y0 = y0 + sy;
            }
        }
    }

    #endregion


    #region Public Method

    public void GotoNextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void GotoPreviousLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    public void RateUsButton()
    {
        Application.OpenURL("https://cafebazaar.ir/app/" + Application.identifier + "/?l=fa");
    }

    public void OtherApps()
    {
        Application.OpenURL("https://cafebazaar.ir/developer/zigool/?l=fa");
    }

    #endregion
}