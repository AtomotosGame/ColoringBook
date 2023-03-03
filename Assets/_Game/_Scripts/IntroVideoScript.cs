using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroVideoScript : MonoBehaviour
{
    public GameObject VideoTexture;
    private float bombTimer = 0f;
    private float endTime = 5f;

    void Awake () {
        DontDestroyOnLoad (transform.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bombTimer += Time.deltaTime;
 
        if(bombTimer > endTime)
            VideoTexture.SetActive(false);
    }

    public void stopVideo() {

    }
}
