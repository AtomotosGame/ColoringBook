using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroVideoScript : MonoBehaviour
{
    private float bombTimer = 0f;
    private float endTime = 10f;
    public static bool videoplay = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bombTimer += Time.deltaTime;
    
        if (!videoplay) 
            gameObject.SetActive(false);

        if(bombTimer > endTime)
            videoplay = false;
        
    }

    public void stopVideo() {

    }
}
