using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject HomePanel;
    public GameObject TutorialPanel;
    public GameObject GamePanel;
    public GameObject WinPanel;
    public GameObject LosePanel;
    public Image FadeEffect;
    public float FadeSpeed = 1.5f;
    public Image TimerBar;
    public float GameTime = 20f;
    public RawImage WinImage;
    public RawImage LoseImage;
    SerialPort serialPort;
    public string portName = "COM4";
    public int baudRate = 9600;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
