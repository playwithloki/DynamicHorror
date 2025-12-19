using UnityEngine;
using UnityEngine.UI;

public class WebcamDisplay : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    private bool isReady = false;

    void Start()
    {
        RawImage rawImage = GetComponent<RawImage>();

        if (WebCamTexture.devices.Length > 0)
        {
            webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name);
            rawImage.texture = webcamTexture;
            rawImage.material.mainTexture = webcamTexture;
            webcamTexture.Play();
        }
        else
        {
            Debug.LogError("No webcam detected!");
        }
    }

    void Update()
    {
        if (webcamTexture != null && webcamTexture.isPlaying && webcamTexture.width > 100 && !isReady)
        {
            Debug.Log("Webcam started: " + webcamTexture.width + "x" + webcamTexture.height);
            isReady = true;
            foreach (var d in WebCamTexture.devices)
            {
                Debug.Log("Camera found: " + d.name);
            }

        }
    }
}
