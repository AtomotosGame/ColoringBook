#if UNITY_EDITOR
using UnityEditor;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(GoogleMobileAdsScript))]
public class GoogleMobileAdsScriptEditor : Editor
{
    public static List<bool> bannerScenes = new List<bool>();
    public static bool showBannerScenes = false;

    public void OnEnable()
    {
        var myScript = target as GoogleMobileAdsScript;

        string bannerString = myScript.bannerScenes;

        bannerScenes.Clear();

        int sceneCount = SceneManager.sceneCountInBuildSettings;

        if (bannerString.Length > sceneCount)
        {
            bannerString = bannerString.Remove(sceneCount, bannerString.Length - sceneCount);
        }
        else if (bannerString.Length < sceneCount)
        {
            int count = sceneCount - bannerString.Length;
            for (int i = 0; i < count; i++)
            {
                bannerString += "0";
            }
        }

        for (int i = 0; i < bannerString.Length; i++)
        {
            bannerScenes.Add(bannerString.Substring(i, 1) == "1" ? true : false);
        }

        myScript.bannerScenes = ListToString(bannerScenes);
    }

    public override void OnInspectorGUI()
    {
        var myScript = target as GoogleMobileAdsScript;

        EditorGUILayout.Space();

        EditorGUILayout.Space();
        myScript.bannerUse = EditorGUILayout.BeginToggleGroup("Banner", myScript.bannerUse);
        using (var group = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(myScript.bannerUse)))
        {
            if (group.visible)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PrefixLabel("Banner Unit ID");
#if UNITY_ANDROID
                myScript.androidBannerUnitID = EditorGUILayout.TextField(myScript.androidBannerUnitID);
#elif UNITY_IOS
                myScript.iOSBannerUnitID = EditorGUILayout.TextField(myScript.iOSBannerUnitID);
#endif

                EditorGUILayout.PrefixLabel("Position");
                myScript.adPosition = (GoogleMobileAds.Api.AdPosition)EditorGUILayout.EnumPopup(myScript.adPosition);

                EditorGUILayout.Space();
                showBannerScenes = EditorGUILayout.Foldout(showBannerScenes, " Active Scenes", showBannerScenes);

                if (showBannerScenes)
                {
                    EditorGUILayout.Space();

                    if (SceneManager.sceneCountInBuildSettings > 0)
                    {
                        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                        {
                            if (bannerScenes.Count != SceneManager.sceneCountInBuildSettings)
                            {
                                bannerScenes.Add(false);
                            }

                            bannerScenes[i] = EditorGUILayout.ToggleLeft(" " + System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)), bannerScenes[i]);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("There is no scene in Build Settings!");
                    }
                }

                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space();
        myScript.interstitialUse = EditorGUILayout.BeginToggleGroup("Interstitial", myScript.interstitialUse);
        using (var group = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(myScript.interstitialUse)))
        {
            if (group.visible)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PrefixLabel("Interstitial Unit ID");
#if UNITY_ANDROID
                myScript.androidInterstitialUnitID = EditorGUILayout.TextField(myScript.androidInterstitialUnitID);
#elif UNITY_IOS
        myScript.iOSInterstitialUnitID = EditorGUILayout.TextField(myScript.iOSInterstitialUnitID);
#endif

                EditorGUILayout.Space();
                myScript.autoInterstitial = EditorGUILayout.BeginToggleGroup("Auto Per Scene", myScript.autoInterstitial);
                using (var group2 = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(myScript.autoInterstitial)))
                {
                    if (group2.visible)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.PrefixLabel("Rate");
                        myScript.interstitialPerSceneNumberRate = EditorGUILayout.IntField(myScript.interstitialPerSceneNumberRate);

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndToggleGroup();

                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space();
        myScript.rewardedUse = EditorGUILayout.BeginToggleGroup("Reward", myScript.rewardedUse);
        using (var group = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(myScript.rewardedUse)))
        {
            if (group.visible)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PrefixLabel("Reward Unit ID");
#if UNITY_ANDROID
                myScript.androidRewardUnitID = EditorGUILayout.TextField(myScript.androidRewardUnitID);
#elif UNITY_IOS
        myScript.iOSRewardUnitID = EditorGUILayout.TextField(myScript.iOSRewardUnitID);
#endif
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space();
        if (GUILayout.Button("Save All"))
        {
            SaveAll();
        }
    }

    void SaveAll()
    {
        var myScript = target as GoogleMobileAdsScript;

        myScript.bannerScenes = ListToString(bannerScenes);

        GameObject gameObject = Selection.activeGameObject;
        //string localPath = "Assets/GoogleMobileAds/" + gameObject.name + ".prefab";
        string localPath = "Assets/_Game/_ad/" + gameObject.name + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction);
    }

    string ListToString(List<bool> list)
    {
        string value = "";
        foreach (bool v in list)
        {
            value += (v ? "1" : "0");
        }

        return value;
    }
}

public static class EditorList
{
    public static void Show(SerializedProperty list)
    {
        if (list.isExpanded)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.ToggleLeft(" " + SceneManager.GetSceneAt(i).name, list.GetArrayElementAtIndex(i).boolValue);
            }
        }
    }
}
#endif