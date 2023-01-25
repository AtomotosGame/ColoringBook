using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_WEBGL
using System.IO;
#endif

public class ScrollListManager : MonoBehaviour
{
    public string saveIndexString = "ColoringList";

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
    private int texHeight = 576;

    private static Dictionary<string, Sprite> allTexturesDic;

    private void Awake()
    {
        if (allTexturesDic == null)
        {
            allTexturesDic = new Dictionary<string, Sprite>();
        }

        firstPos = PlayerPrefs.GetInt(saveIndexString, 0);

        lerping = false;
        buttonPressed = false;

        // Set size of the cell
        if (GetComponent<GridLayoutGroup>().cellSize == Vector2.zero)
        {
            Vector2 cellSize = new Vector2(cellSizeX, cellSizeY);
            GetComponent<GridLayoutGroup>().cellSize = cellSize;
        }
        else
        {
            cellSizeX = GetComponent<GridLayoutGroup>().cellSize.x;
            cellSizeY = GetComponent<GridLayoutGroup>().cellSize.y;
        }

        // Set size delta of parent scroll rect so elements wouldn't be jumpy
        transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(cellSizeX, cellSizeY);

        if (horizontalList)
        {
            transform.parent.GetComponent<ScrollRect>().horizontal = true;
            transform.parent.GetComponent<ScrollRect>().vertical = false;

            // Check if layout spacing differes from zero vector
            if (GetComponent<GridLayoutGroup>().spacing == Vector2.zero)
            {
                Vector2 spacingVector = new Vector2(spacing, 0);
                GetComponent<GridLayoutGroup>().spacing = spacingVector;
            }
            else
            {
                if (GetComponent<GridLayoutGroup>().spacing.x != 0)
                    spacing = GetComponent<GridLayoutGroup>().spacing.x;
            }

            GetComponent<GridLayoutGroup>().startAxis = GridLayoutGroup.Axis.Vertical;
            GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.FixedRowCount;
            GetComponent<GridLayoutGroup>().constraintCount = 1;
            currentCharCheckTemp = (cellSizeX + spacing) / 2;
        }
        else
        {
            transform.parent.GetComponent<ScrollRect>().horizontal = false;
            transform.parent.GetComponent<ScrollRect>().vertical = true;

            if (GetComponent<GridLayoutGroup>().spacing == Vector2.zero)
            {
                Vector2 spacingVector = new Vector2(0, spacing);
                GetComponent<GridLayoutGroup>().spacing = spacingVector;
            }
            else
            {
                if (GetComponent<GridLayoutGroup>().spacing.y != 0)
                    spacing = GetComponent<GridLayoutGroup>().spacing.y;
            }

            GetComponent<GridLayoutGroup>().startAxis = GridLayoutGroup.Axis.Horizontal;
            GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            GetComponent<GridLayoutGroup>().constraintCount = 1;
            currentCharCheckTemp = (cellSizeY + spacing) / 2;
        }

        snapPositions = new List<float>();
        listOfCharacters = new List<GameObject>();

        // Get all characters and put then into list
        foreach (Transform t in transform)
            listOfCharacters.Add(t.gameObject);

        // Set transform rect position and size depending of number of characters and spacing
        if (horizontalList)
        {
            GetComponent<RectTransform>().sizeDelta = new Vector2(listOfCharacters.Count * cellSizeX + (listOfCharacters.Count - 1) * spacing, cellSizeY);
            GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().sizeDelta.x - 2 * spacing, GetComponent<RectTransform>().anchoredPosition.y);

            float startSnapPosition = GetComponent<RectTransform>().sizeDelta.x / 2 - cellSizeX / 2;
            snapPositions.Add(startSnapPosition);

            // Set fist character to be of focused scale
            listOfCharacters[0].transform.localScale = new Vector3(focusedElementScale, focusedElementScale, 1);

            for (int i = 1; i < listOfCharacters.Count; i++)
            {
                startSnapPosition -= cellSizeX + spacing;
                snapPositions.Add(startSnapPosition);

                // Set scale for not focused elements to be scale
                listOfCharacters[i].transform.localScale = new Vector3(unfocusedElementsScale, unfocusedElementsScale, 1);
            }
        }
        else
        {
            GetComponent<RectTransform>().sizeDelta = new Vector2(cellSizeX, listOfCharacters.Count * cellSizeY + (listOfCharacters.Count - 1) * spacing);
            GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().anchoredPosition.x, -(GetComponent<RectTransform>().sizeDelta.y - 2 * spacing));

            float startSnapPosition = GetComponent<RectTransform>().sizeDelta.y / 2 - cellSizeY / 2;
            snapPositions.Add(startSnapPosition);

            // Set fist character to be of focused scale
            listOfCharacters[0].transform.localScale = new Vector3(focusedElementScale, focusedElementScale, 1);

            for (int i = 1; i < listOfCharacters.Count; i++)
            {
                startSnapPosition -= cellSizeY + spacing;
                snapPositions.Add(startSnapPosition);

                // Set scale for not focused elements to be scale
                listOfCharacters[i].transform.localScale = new Vector3(unfocusedElementsScale, unfocusedElementsScale, 1);
            }
        }

        SetNewPos(firstPos);

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

    private void LoadAllTexture()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Image>().sprite = LoadImage(saveIndexString + i.ToString(), saveIndexString + i.ToString() == ColoringBookManager.ID);
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

    // Determining closesst snap point -349 is half distance - 1 and 350 is half distance
    private void SetLerpPositionToClosestSnapPoint()
    {
        for (int i = 0; i < snapPositions.Count; i++)
        {
            if (horizontalList)
            {
                if (transform.localPosition.x > snapPositions[i] - currentCharCheckTemp - 1 && transform.localPosition.x <= snapPositions[i] + currentCharCheckTemp)
                {
                    newLerpPosition = new Vector3(snapPositions[i], 0, 0);
                    lerping = true;
                    currentCharacter = i;
                    break;
                }
            }
            else
            {
                if (transform.localPosition.y > snapPositions[i] - currentCharCheckTemp - 1 && transform.localPosition.y <= snapPositions[i] + currentCharCheckTemp)
                {
                    newLerpPosition = new Vector3(0, snapPositions[i], 0);
                    lerping = true;
                    currentCharacter = listOfCharacters.Count - i - 1;
                    break;
                }
            }
        }
    }

    private void SetCurrentCharacter()
    {
        for (int i = 0; i < snapPositions.Count; i++)
        {
            if (horizontalList)
            {
                if (transform.localPosition.x > snapPositions[i] - currentCharCheckTemp - 1 && transform.localPosition.x <= snapPositions[i] + currentCharCheckTemp)
                {
                    currentCharacter = i;
                    break;
                }
            }
            else
            {
                if (transform.localPosition.y > snapPositions[i] - currentCharCheckTemp - 1 && transform.localPosition.y <= snapPositions[i] + currentCharCheckTemp)
                {
                    currentCharacter = listOfCharacters.Count - i - 1;
                    break;
                }
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

    private void LateUpdate()
    {
        // If we are holding button than do not lerp
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !buttonPressed)
        {
            SetCurrentCharacter();
            newLerpPosition = transform.localPosition;
        }

        // If not lerping and velocityis small enough find closest snap point and lerp to it
        if (horizontalList)
        {
            if (!lerping && Mathf.Abs(transform.parent.GetComponent<ScrollRect>().velocity.x) >= 0f && Mathf.Abs(transform.parent.GetComponent<ScrollRect>().velocity.x) < 100f)
            {
                SetLerpPositionToClosestSnapPoint();
            }
            else
            {
                SetCurrentCharacter();
            }
        }
        else
        {
            if (!lerping && Mathf.Abs(transform.parent.GetComponent<ScrollRect>().velocity.y) >= 0f && Mathf.Abs(transform.parent.GetComponent<ScrollRect>().velocity.y) < 100f)
            {
                SetLerpPositionToClosestSnapPoint();
            }
            else
            {
                SetCurrentCharacter();
            }
        }

        // Set appropriate for elements in list according to distance from current snap point
        if (horizontalList)
        {
            if (currentCharacter == 0)
            {
                float sb = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] - transform.localPosition.x - currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float s = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] - transform.localPosition.x) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);

                if (s <= unfocusedElementsScale || s > focusedElementScale)
                    s = unfocusedElementsScale;

                if (sb <= unfocusedElementsScale || sb > focusedElementScale)
                    sb = unfocusedElementsScale;

                listOfCharacters[currentCharacter].transform.localScale = new Vector3(s, s, 1);

                listOfCharacters[currentCharacter + 1].transform.localScale = new Vector3(sb, sb, 1);
            }
            else if (currentCharacter == listOfCharacters.Count - 1)
            {
                float s = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] - transform.localPosition.x) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float sf = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] - transform.localPosition.x + currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);

                if (s <= unfocusedElementsScale || s > focusedElementScale)
                    s = unfocusedElementsScale;

                if (sf <= unfocusedElementsScale || sf > focusedElementScale)
                    sf = unfocusedElementsScale;

                listOfCharacters[currentCharacter - 1].transform.localScale = new Vector3(sf, sf, 1);
                listOfCharacters[currentCharacter].transform.localScale = new Vector3(s, s, 1);
            }
            else
            {
                float sb = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] - transform.localPosition.x - currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float s = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] - transform.localPosition.x) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float sf = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] - transform.localPosition.x + currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);

                if (s <= unfocusedElementsScale || s > focusedElementScale)
                    s = unfocusedElementsScale;

                if (sb <= unfocusedElementsScale || sb > focusedElementScale)
                    sb = unfocusedElementsScale;

                if (sf <= unfocusedElementsScale || sf > focusedElementScale)
                    sf = unfocusedElementsScale;

                listOfCharacters[currentCharacter - 1].transform.localScale = new Vector3(sf, sf, 1);
                listOfCharacters[currentCharacter].transform.localScale = new Vector3(s, s, 1);
                listOfCharacters[currentCharacter + 1].transform.localScale = new Vector3(sb, sb, 1);
            }
        }
        else
        {
            if (currentCharacter == 0)
            {
                float sb = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] + transform.localPosition.y - currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float s = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] + transform.localPosition.y) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);

                if (s <= unfocusedElementsScale || s > focusedElementScale)
                    s = unfocusedElementsScale;

                if (sb <= unfocusedElementsScale || sb > focusedElementScale)
                    sb = unfocusedElementsScale;

                listOfCharacters[currentCharacter].transform.localScale = new Vector3(s, s, 1);
                listOfCharacters[currentCharacter + 1].transform.localScale = new Vector3(sb, sb, 1);
            }
            else if (currentCharacter == listOfCharacters.Count - 1)
            {
                float s = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] + transform.localPosition.y) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float sf = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] + transform.localPosition.y + currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);

                if (s <= unfocusedElementsScale || s > focusedElementScale)
                    s = unfocusedElementsScale;

                if (sf <= unfocusedElementsScale || sf > focusedElementScale)
                    sf = unfocusedElementsScale;

                listOfCharacters[currentCharacter - 1].transform.localScale = new Vector3(sf, sf, 1);
                listOfCharacters[currentCharacter].transform.localScale = new Vector3(s, s, 1);
            }
            else
            {
                float sb = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] + transform.localPosition.y - currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float s = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] + transform.localPosition.y) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);
                float sf = Mathf.Abs(Mathf.Abs(snapPositions[currentCharacter] + transform.localPosition.y + currentCharCheckTemp * 2) * (focusedElementScale - unfocusedElementsScale) / Mathf.Abs(currentCharCheckTemp * 2) - focusedElementScale);

                if (s <= unfocusedElementsScale || s > focusedElementScale)
                    s = unfocusedElementsScale;

                if (sb <= unfocusedElementsScale || sb > focusedElementScale)
                    sb = unfocusedElementsScale;

                if (sf <= unfocusedElementsScale || sf > focusedElementScale)
                    sf = unfocusedElementsScale;

                listOfCharacters[currentCharacter - 1].transform.localScale = new Vector3(sf, sf, 1);
                listOfCharacters[currentCharacter].transform.localScale = new Vector3(s, s, 1);
                listOfCharacters[currentCharacter + 1].transform.localScale = new Vector3(sb, sb, 1);
            }
        }

        // If we let the mouse button and velocity small enough
        if (lerping)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, newLerpPosition, lerpingSpeed);

            if (Vector3.Distance(transform.localPosition, newLerpPosition) < 1f)
            {
                transform.localPosition = newLerpPosition;
                transform.parent.GetComponent<ScrollRect>().velocity = new Vector3(0, 0, 0);
                lerping = false;

                for (int i = 0; i < listOfCharacters.Count; i++)
                {
                    if (i != currentCharacter)
                        listOfCharacters[i].transform.localScale = new Vector3(unfocusedElementsScale, unfocusedElementsScale, 1);
                }

            }
        }

        if (horizontalList)
        {
            // Updating arrow buttons
            if (transform.localPosition.x > snapPositions[snapPositions.Count - 1] - spacing / 2)
            {
                SetButtonActive(forwardButton);
            }
            else
            {
                SetButtonInactive(forwardButton);
            }

            if (transform.localPosition.x < snapPositions[0] + spacing / 2)
            {
                SetButtonActive(backwardButton);
            }
            else
            {
                SetButtonInactive(backwardButton);
            }
        }
        else
        {
            // Updating arrow buttons
            if (transform.localPosition.y > snapPositions[snapPositions.Count - 1] - spacing / 2)
            {
                SetButtonActive(backwardButton);
            }
            else
            {
                SetButtonInactive(backwardButton);
            }

            if (transform.localPosition.y < snapPositions[0] + spacing / 2)
            {
                SetButtonActive(forwardButton);
            }
            else
            {
                SetButtonInactive(forwardButton);
            }
        }
    }

    public void LoadGame(int index)
    {
        MusicController.USE.PlaySound(MusicController.USE.clickSound);

        PlayerPrefs.SetInt(saveIndexString, index);
        PlayerPrefs.Save();

        if (transform.GetChild(index).childCount > 0)
        {
            ColoringBookManager.maskTexIndex = index;
        }
        else
        {
            ColoringBookManager.maskTexIndex = -1;
        }

        ColoringBookManager.ID = saveIndexString + index.ToString();
        SceneManager.LoadScene("PaintScene");
    }
}