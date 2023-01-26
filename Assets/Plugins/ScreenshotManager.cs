#pragma warning disable 0168 // variable declared but not used.
#pragma warning disable 0219 // variable assigned but not used.

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

public class ScreenshotManager : MonoBehaviour
{
    public static event Action ScreenshotFinishedSaving;

#if UNITY_IPHONE
	[DllImport("__Internal")]
    private static extern bool saveToGallery( string path );
#elif UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void ImageDownloader(string str, string fn);

    private static void DownloadScreenshot(byte[] imageData, string imageFilename)
    {
        ImageDownloader(System.Convert.ToBase64String(imageData), imageFilename);
    }
#endif


    public static IEnumerator SaveForPaint(string fileName, string albumName = "MyScreenshots", bool callback = false)
    {
        bool photoSaved = false;

        string date = System.DateTime.Now.ToString("dd-MM-yy");

        ScreenshotManager.ScreenShotNumber++;

        string screenshotFilename = fileName + "_" + ScreenshotManager.ScreenShotNumber + "_" + date + ".jpg";

        Rect rect = new Rect(0, Screen.height * 0.2f, Screen.width * 0.8f, Screen.height * 0.8f);

        Debug.Log("Save screenshot " + screenshotFilename);

        string share = "I drew this painting! ^_^";
        share += '\n' + "Do you like drawing too? Then install Coloring Book!";

#if UNITY_IPHONE
		
			if(Application.platform == RuntimePlatform.IPhonePlayer) 
			{
				Debug.Log("iOS platform detected");
				
				string iosPath = Application.persistentDataPath + "/" + screenshotFilename;
		
				//Application.CaptureScreenshot(screenshotFilename);
                
                //////////////
                yield return new WaitForEndOfFrame();

                Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
                texture.ReadPixels(rect, 0, 0);

                texture.Apply();

                yield return 0;

                byte[] bytes = texture.EncodeToJPG();
                File.WriteAllBytes(iosPath, bytes);

                Destroy(texture);
                //////////////
				
				while(!photoSaved) 
				{
					photoSaved = saveToGallery( iosPath );
					
					yield return new WaitForSeconds(.5f);
				}
			
				UnityEngine.iOS.Device.SetNoBackupFlag( iosPath );
			
                new NativeShare().AddFile(iosPath).SetSubject(albumName).SetText(share).Share();
			} 
            else
            {
                //Application.CaptureScreenshot(screenshotFilename);

                //////////////
                yield return new WaitForEndOfFrame();

                Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
                texture.ReadPixels(rect, 0, 0);

                texture.Apply();

                yield return 0;

                byte[] bytes = texture.EncodeToJPG();
                File.WriteAllBytes(Application.persistentDataPath + "/" + screenshotFilename, bytes);

                Destroy(texture);
                //////////////
            }
			
#elif UNITY_ANDROID

        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("Android platform detected");

            yield return new WaitForEndOfFrame();

            Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            texture.ReadPixels(rect, 0, 0);
            texture.Apply();

            yield return 0;

            byte[] bytes = texture.EncodeToJPG();

            string path = Path.Combine(Application.temporaryCachePath, screenshotFilename);
            File.WriteAllBytes(path, bytes);

            NativeGallery.SaveImageToGallery(bytes, albumName, screenshotFilename);

            Destroy(texture);

            new NativeShare().AddFile(path).SetSubject(albumName).SetText(share).Share();
        }
        else
        {
            //////////////
            yield return new WaitForEndOfFrame();

            Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            texture.ReadPixels(rect, 0, 0);

            texture.Apply();

            yield return 0;

            byte[] bytes = texture.EncodeToJPG();
            File.WriteAllBytes(Application.persistentDataPath + "/" + screenshotFilename, bytes);

            Destroy(texture);
            //////////////
        }

#elif UNITY_WEBGL

        yield return new WaitForEndOfFrame();

        Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        texture.ReadPixels(rect, 0, 0);

        texture.Apply();

        yield return 0;

        byte[] bytes = texture.EncodeToJPG();

        DownloadScreenshot(bytes, screenshotFilename);

        Destroy(texture);

#else
        while (!photoSaved)
        {
            yield return new WaitForSeconds(.5f);

            photoSaved = true;
        }
#endif
        if (callback)
            ScreenshotFinishedSaving();
    }

    public static int ScreenShotNumber
    {
        set { PlayerPrefs.SetInt("screenShotNumber", value); }

        get { return PlayerPrefs.GetInt("screenShotNumber"); }
    }
}
