using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EmotionClient_Min : MonoBehaviour
{
    public RawImage webcamDisplay;
    public Text emotionText;
    public string serverURL = "http://127.0.0.1:5000/analyze";
    public float sendEvery = 1.5f;

    public string emotion;

    WebCamTexture cam;

    void Start()
    {
        Debug.Log("[EmotionClient_Min] Start");
        if (WebCamTexture.devices.Length == 0) { Debug.LogError("No webcam"); return; }
        cam = new WebCamTexture(WebCamTexture.devices[0].name, 640, 480);
        if (webcamDisplay) { webcamDisplay.texture = cam; webcamDisplay.material.mainTexture = cam; }
        cam.Play();
        StartCoroutine(InitThenLoop());
    }

    IEnumerator InitThenLoop()
    {
        while (cam.width < 100) yield return null;
        Debug.Log("[EmotionClient_Min] Webcam ready " + cam.width + "x" + cam.height);
        while (true)
        {
            yield return StartCoroutine(SendOne());
            yield return new WaitForSeconds(sendEvery);
        }
    }

    IEnumerator SendOne()
    {
        // capture
        Texture2D tex = new Texture2D(cam.width, cam.height, TextureFormat.RGB24, false);
        tex.SetPixels(cam.GetPixels());
        tex.Apply();
        byte[] jpg = tex.EncodeToJPG(80);
        Destroy(tex);

        string b64 = Convert.ToBase64String(jpg);
        string json = "{\"image\":\"" + b64 + "\"}";

        using (var req = new UnityWebRequest(serverURL, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 30;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[EmotionClient_Min] HTTP error: " + req.error);
                if (emotionText) emotionText.text = "request_error";
            }
            else
            {
                string body = req.downloadHandler.text;
                emotion = body;
  
                Debug.Log("[EmotionClient_Min] Response: " + body);
                
                string emo = ExtractDominant(body) ?? "unknown";
                if (emotionText) emotionText.text = emo;
            }
        }
    }

    string ExtractDominant(string json)
    {
        const string key = "\"dominant_emotion\"";
        int i = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        int c = json.IndexOf(':', i);
        int q1 = json.IndexOf('"', c + 1);
        int q2 = json.IndexOf('"', q1 + 1);
        if (q1 < 0 || q2 < 0) return null;
        return json.Substring(q1 + 1, q2 - q1 - 1);
    }
}
