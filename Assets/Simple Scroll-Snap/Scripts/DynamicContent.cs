// Simple Scroll-Snap - https://assetstore.unity.com/packages/tools/gui/simple-scroll-snap-140884
// Copyright (c) Daniel Lochner

using System;
using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class DynamicContent : MonoBehaviour
    {
        #region Fields
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private Toggle togglePrefab;
        [SerializeField] private ToggleGroup toggleGroup;
        [SerializeField] private InputField addInputField, removeInputField;
        [SerializeField] private SimpleScrollSnap scrollSnap;
        public string saveIndexString = "allDrawItem";
        public Transform ScrolllistColoringObj;

        private float toggleWidth;
        #endregion

        #region Methods
        private void Awake()
        {
            toggleWidth = (togglePrefab.transform as RectTransform).sizeDelta.x * (Screen.width / 2048f); ;
        }

        void Start() {
            if (saveIndexString == "allDrawItem")
                createPanels();
            else 
                createColoringPanels();
        }

        public void createPanels() {
            int allItemNum = PlayerPrefs.GetInt(saveIndexString);
            Debug.Log(allItemNum);
            int panelNum =  (int) (Mathf.Floor((allItemNum+1)/10)) + 1;

            if (allItemNum == 0) panelNum = 1;

            for (int i = 0; i < panelNum; i++) {  
                AddAtIndex();
            }
            ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().RemoveItems();
            ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().RenamePanel();
            ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().LoadAllTexture();
        }
    
        public void createColoringPanels() {

            int selectedNum = ScrollListManagerColoring.selectedcolorItem;
            int allItemNum = ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().coloringItems[selectedNum].fileNumber;
            int panelNum =  (int) (Mathf.Floor((allItemNum)/10)) + 1;

            if (allItemNum/10 == 0) panelNum--;

            for (int i = 0; i < panelNum; i++) {  
                AddAtIndex();
            }
            ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().RenamePanel();
            ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().LoadAllColorTexture();
            ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().ColoringRemoveItems();
            ScrolllistColoringObj.GetComponent<ScrollListManagerColoring>().GetFirebaseData();
        }

        public void Add(int index)
        {
            // Pagination
            Toggle toggle = Instantiate(togglePrefab, scrollSnap.Pagination.transform.position + new Vector3(toggleWidth * (scrollSnap.NumberOfPanels + 1), 0, 0), Quaternion.identity, scrollSnap.Pagination.transform);
            toggle.group = toggleGroup;
            scrollSnap.Pagination.transform.position -= new Vector3(toggleWidth / 2f, 0, 0);
            // Panel
            // panelPrefab.GetComponent<Image>().color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            scrollSnap.Add(panelPrefab, index);
        }
        public void AddAtIndex()
        {
            Add(Convert.ToInt32("0"));
        }
        public void AddToFront()
        {
            Add(0);
        }
        public void AddToBack()
        {
            Add(scrollSnap.NumberOfPanels);
        }

        public void Remove(int index)
        {
            if (scrollSnap.NumberOfPanels > 0)
            {
                // Pagination
                DestroyImmediate(scrollSnap.Pagination.transform.GetChild(scrollSnap.NumberOfPanels - 1).gameObject);
                scrollSnap.Pagination.transform.position += new Vector3(toggleWidth / 2f, 0, 0);

                // Panel
                scrollSnap.Remove(index);
            }
        }
        public void RemoveAtIndex()
        {
            Remove(Convert.ToInt32("0"));
        }
        public void RemoveFromFront()
        {
            Remove(0);
        }
        public void RemoveFromBack()
        {
            if (scrollSnap.NumberOfPanels > 0)
            {
                Remove(scrollSnap.NumberOfPanels - 1);
            }
            else
            {
                Remove(0);
            }
        }
        #endregion
    }
}