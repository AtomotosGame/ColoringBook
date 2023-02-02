using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadGameButtonScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DrawLoadGame(int num) {
        int index = int.Parse(transform.name)*10 + num;
        ScrollListManagerColoring.LoadGame(index);
    }
}
