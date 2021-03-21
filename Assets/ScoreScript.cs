using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreScript : MonoBehaviour
{

    public int ScoreValue = 0;
    public string PlayerName = "";
    Text score;
    // Start is called before the first frame update
    void Start()
    {
        score = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        score.text = PlayerName + " Score: " + ScoreValue;
    }
}
