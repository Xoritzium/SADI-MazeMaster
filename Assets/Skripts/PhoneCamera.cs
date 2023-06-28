using System;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PhoneCamera : MonoBehaviour
{
    // Start is called before the first frame update

    private WebCamTexture webCam;
    private Texture defaultBackground;

    public RawImage background;
    public AspectRatioFitter fit;
    public RawImage preview;
    public TMP_Text height;
    public TMP_Text width;

    private WebCamDevice[] devices;




    private void Start() {

        defaultBackground = background.texture;
        devices = WebCamTexture.devices;

        if (devices.Length == 0) {
            Debug.Log("no cam found!");
            return;
        }


        webCam = new WebCamTexture(devices[0].name); // screen size
        Debug.Log("webcam height: " + webCam.height);
        Debug.Log("webcam width: " + webCam.width);


        if (webCam == null) {
            Debug.Log("no backcam");
            return;
        }
        webCam.Play();
        Texture2D tex = new Texture2D(webCam.width, webCam.height);
        tex.SetPixels(webCam.GetPixels());
        background.texture = tex;
        string webcamHeight = "height: " + webCam.height;
        string webcamWidth = "width: " + webCam.width;
        height.GetComponent<TextMeshProUGUI>().text = webcamHeight;
        width.GetComponent<TextMeshProUGUI>().text = webcamWidth;



    }


    private void Update() {
        background.texture = webCam;
    }


    public Texture2D createFinalImage(Texture2D img) {
        Texture2D final = new Texture2D(img.height, img.height);
        for (int x = 0; x < img.width; x++) {
            for (int y = 0; y < img.height; y++) {



                if (x < img.width && x < img.height) {
                    final.SetPixel(x, y, img.GetPixel(x, y));
                }

                // delete. debug purpose
                if (x < 20 && y < 20) {
                    final.SetPixel(x, y, Color.red);
                }
            }
        }
        return final;
    }




    public void createImage() {

        Color[] webCamPixels = webCam.GetPixels();
        Texture2D img = new Texture2D(webCam.width, webCam.height);
        img.SetPixels(webCamPixels);
        img = createFinalImage(img);
        // hier ist job erfüllt. ImageTexture x² für dings


        byte[] byteArray = img.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Resources/" + "Texture2D" + ".png", byteArray);
        Debug.LogFormat("image from Camera safed to " + Application.dataPath + "/" + "save2DTexture");

        Texture2D tex = img;
        byte[] fileData;
        string filePath = Application.dataPath + "/tests/" + "save2DTexture" + ".png";
        if (System.IO.File.Exists(filePath)) {
            fileData = System.IO.File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        preview.texture = tex;


    }

}
