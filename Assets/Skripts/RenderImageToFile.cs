using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;


public class RenderImageToFile : MonoBehaviour
{

    /* -> ich kann nach einer spezifischen Farbe suchen, da die Fokusbox intern ist und das gerenderte Bild ebenfalls.
     -> Den "rotesten Pixel finden"(alternativ-Idee)
    -> Pixel vergleichen -> X und Y rauskriegen (avg x und avg Y) so kann dann alles 
        was kleiner und groeßer ist, weggesaebelt werden
    -> Alternative: Funktion die die Baseplate vorher einliest (Feature..)
    -> Fokus box geht einfach so nicht als Teil des Canvas.

    */
    /*
    Conclusion so far:+
    -> rgbs vergleichen mit trehshold von 0,38 bei irgendwas ungefähr grünem ist relativ akkurat, 
        filtert aber nicht die dunklen anteile raus.
    ->
    -> border berechnung tut noch nicht.
    */



    [SerializeField]
    string fileName, filePath;
    [SerializeField]
    int pictureWidth, pictureHeight;
    [SerializeField]
    Image visualizeWebcam;
    [SerializeField]
    Image testImage;

    private WebCamTexture webcam;
    private bool takePicture;
    [SerializeField]
    private float firstThreshold, secondThreshold, lightnessFactor, pixelsToBeFound;
    [SerializeField, Range(0, 1)]
    float influenceOfLightness;



    private int bakeTextureWidth, bakeTextureHeight;



    private void Awake() {
        webcam = new WebCamTexture();
        webcam.Play();
        pictureWidth = webcam.width;
        pictureHeight = webcam.height;
        //       Camera.onPostRender += renderImage;
        Texture2D img = new Texture2D(webcam.width, webcam.height);
        visualizeWebcam.material.mainTexture = webcam;

    }

    private void performStuff() {

        Color[] fromWebcam = getPixelsFromWebcam();

        bakeColorToFile(fromWebcam, "original", 0);

        //abstraction of colors
        Color[] whiteFiltered = filterPixelArray(fromWebcam, new(0.231f, 0.012f, 0.025f), firstThreshold);
        bakeColorToFile(whiteFiltered, "filterPixelArray", firstThreshold);

        //  whiteFiltered = createBorders(whiteFiltered, fromWebcam);
        // bakeColorToFile(whiteFiltered, "afterFilter-whiteFilter-threshold", firstThreshold);


        /*
        Color[] afterFilter = filterPixelArray(fromWebcam, new(0.105f, 0.283f, 0.181f), firstThreshold);
        bakeColorToFile(afterFilter, "afterFilter-threshold", firstThreshold);

        increaseLightnessOfPicture(afterFilter);
        bakeColorToFile(afterFilter, "afterFilter-threshold-Lightness-", lightnessFactor);

        afterFilter = filterPixelArray(fromWebcam, new(0.143f, 0.301f, 0.225f), secondThreshold);
        bakeColorToFile(afterFilter, "afterFilter-Second-threshold", secondThreshold);
        Texture2D filtered = new(1920, 1080);
        filtered.SetPixels(afterFilter);
        //      testImage.material.mainTexture = filtered;

        bakeColorToFile(fromWebcam, "original", 0);
        */
    }

    private Color[] getPixelsFromWebcam() {
        Color[] pixels = webcam.GetPixels();
        Debug.Log("pixelSize from webcam-> expected: 720*480=34599 - actual: " + pixels.Length);
        return pixels;
    }
    /**
     * increases the lightness of the picture as color correction
     */
    private void increaseLightnessOfPicture(Color[] pixels) {
        float h, s, v;
        for (int i = 0; i < pixels.Length; i++) {
            Color.RGBToHSV(pixels[i], out h, out s, out v);
            pixels[i] = Color.HSVToRGB(h, s, 1);
        }
    }



    private void bakeColorToFile(Color[] pixels, string fileName, float currentThreshold) {

        Texture2D texture = new(webcam.width, webcam.height); // muss webcam x,y sein
        texture.SetPixels(pixels);
        byte[] byteArray = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/tests/" + fileName + "-" + currentThreshold + ".png", byteArray);
        Debug.LogFormat("image from Camera safed to " + Application.dataPath + "/" + fileName);
    }

    /**
     * converts both color into hsv and compares the distance
     * Value of the Colors must be between 0,1
     */
    private float DistanceBetweenColors(Color a, Color b) {
        float aH, aS, aV, bH, bS, bV;
        Color.RGBToHSV(a, out aH, out aS, out aV);
        Color.RGBToHSV(b, out bH, out bS, out bV);
        aV = 1;
        bV = 1;

        //hsv
        float x = Mathf.Pow(bH - aH, 2);
        float y = Mathf.Pow(bS - aS, 2);
        float z = Mathf.Pow(bV - aV, 2) * influenceOfLightness;  // add weight to Value/ how much light affects the output
        return Mathf.Sqrt(x + y + z);
    }


    /**
     * filters the given Color array onto a specific color, all value inside the color must be betweeen 0,1;
     */
    private Color[] filterPixelArray(Color[] originalPixels, Color referenceColor, float threshold) {
        Color[] foundPixels = new Color[originalPixels.Length];

        for (int x = 0; x < pictureWidth; x++) {    // iterate through width
            for (int y = 0; y < pictureHeight; y++) {//iterate through height
                int loc = x + (y * pictureWidth);
                int counter = 0;
                if (x > 0 && x < pictureWidth && y > 0 && y < pictureHeight) {
                    for (int i = x - 1; i < x + 1; i++) { //3x3 around pixel
                        for (int j = y - 1; j < y + 1; j++) {
                            int tmpLoc = i + (j * pictureWidth);
                            if (DistanceBetweenColors(originalPixels[tmpLoc], referenceColor) < threshold) { // check on colorMatch
                                counter++;
                            }
                        }
                    }
                }
                if (counter > 3) {  // genug umliegende Pixel da
                    foundPixels[loc] = originalPixels[loc];
                } else {
                    foundPixels[loc] = Color.magenta;
                }
            }
        }
        bakeColorToFile(foundPixels, "temp-Print-tempory-Found-Pixels", threshold);     //temp look.

        int[] borders = createBoxborders(foundPixels);
       // { down,up,left,right} -> result
        Color[] filteredPixels = new Color[originalPixels.Length]; // theoretisch müsste es später reichen, nur die Pixel zu schreiben, die in final Pixels stehen!
        for (int x = 0; x < pictureWidth; x++) {    // iterate through width
            for (int y = 0; y < pictureHeight; y++) {//iterate through height
                int loc = x + (y * pictureWidth);

                if (x >= borders[0] && x <= borders[1] && y >= borders[2] && y <= borders[3]) {
                    filteredPixels[loc] = originalPixels[loc];
                } else {
                    filteredPixels[loc] = Color.gray;
                }
            }
        }
        return filteredPixels;
    }
    /*
     * X,Y coordinates of all colors for the border.
     * calculate the final borders of the playfield
     */
    private int[] createBoxborders(Color[] pixels) {
        int down = 0, up = 0, left = 0, right = 0; // final borders.


        for (int x = 0; x < pictureWidth; x++) {    // iterate through width
            for (int y = 0; y < pictureHeight; y++) {//iterate through height
                int loc = x + (y * pictureWidth);
                int countX = 0, countY = 0;
                float distanceCurrentColor = DistanceBetweenColors(pixels[loc], Color.magenta);
                if (distanceCurrentColor != 0) {

                    //x direction
                    for (int i = x; i < pictureWidth; i++) {
                        int tmpLoc = i + (y * pictureWidth);
                        distanceCurrentColor = DistanceBetweenColors(pixels[tmpLoc], Color.magenta);
                        if (distanceCurrentColor != 0) {
                            countX++;
                        }
                    }
                    if (countX > pixelsToBeFound) { // 200 

                        if (x <= down) {
                            down = y;
                        } else if (x >= up) {
                            up = x;
                        }
                    }
                    /// y direction
                    for (int i = y; y < pictureHeight; i++) {
                        int tmpLoc = x + (y * pictureWidth);
                        distanceCurrentColor = DistanceBetweenColors(pixels[tmpLoc], Color.magenta);
                        if (distanceCurrentColor != 0) {
                            countY++;
                        }
                    }
                    if (countY > pixelsToBeFound) { // 200 -> weil quadratisches Spielfeld

                        if (y<= left) {
                            left = y;
                        } else if (y >=right) {
                            right = y; ;
                        }
                    }
                }

            }
        }
        int[] ret = { down,up,left,right};
        return ret;

    }



    /**
     * replace all not referenceColor matching pixels with white pixels.
     */
    private Color[] whiteFilteredPixels(Color[] pixels, Color referenceColor, float threshold) {
        Color[] filteredPixels = new Color[pixels.Length];
        for (int x = 0; x < pictureWidth; x++) {
            for (int y = 0; y < pictureHeight; y++) {
                int loc = x + (y * pictureWidth);
                if (DistanceBetweenColors(pixels[loc], referenceColor) < threshold) {
                    filteredPixels[loc] = pixels[loc];
                } else {
                    filteredPixels[loc] = Color.white;
                }
            }
        }
        return filteredPixels;
    }

    private Color[] clearColorFilter(Color[] pixels, Color referenceColor, float threshold) {


        Color[] filteredPixels = new Color[pixels.Length];
        for (int x = 0; x < pictureWidth; x++) {
            for (int y = 0; y < pictureHeight; y++) {

                int loc = x + (y * pictureWidth);
                if (x > 0 && x < pictureWidth && y > 0 && y < pictureHeight) {
                    int counter = 0;
                    for (int i = x - 1; i < x + 1; i++) { //3x3 around pixel
                        for (int j = y - 1; j < y + 1; j++) {
                            int tmpLoc = i + (j * pictureWidth);
                            if (DistanceBetweenColors(pixels[tmpLoc], referenceColor) < threshold) {
                                counter++;
                            }
                        }
                    }
                    if (counter > 3) {  // genug umliegende Pixel da
                        filteredPixels[loc] = pixels[loc];
                    } else {
                        filteredPixels[loc] = Color.magenta;
                    }
                }

            }
        }
        return filteredPixels;
    }

    private Color[] createBorders(Color[] pixels, Color[] original) {
        int borderX = 0;
        int borderY = 0;
        int currentHSX = 0;
        int currentHSY = 0;
        int countXdir = 0;
        int countYdir = 0;
        for (int x = 0; x < pictureWidth; x++) {
            for (int y = 0; y < pictureHeight; y++) {
                int loc = x + (y * pictureWidth);

                if ((isPixelWhite(pixels[loc]))) {

                    for (int i = x; i < pictureWidth; i++) {    // count in x direction
                        int tmpLoc = i + (y * pictureWidth);
                        if (isPixelWhite(pixels[tmpLoc])) {
                            countXdir++;
                        }
                    }
                    if (countXdir > currentHSX) {
                        borderX = x;
                        currentHSX = countXdir;
                    }
                    for (int i = y; i < pictureHeight; i++) { // count in y direction
                        int tmpLoc = x + (i * pictureWidth);
                        if (isPixelWhite(pixels[tmpLoc])) {
                            countYdir++;
                        }
                    }
                    if (countYdir > currentHSY) {
                        borderY = y;
                        currentHSY = countYdir;
                    }
                } else {
                    countXdir = 0;
                    countYdir = 0;
                }


            }
        }

        Color[] final = new Color[pixels.Length];
        for (int x = 0; x < pictureWidth; x++) {
            for (int y = 0; y < pictureHeight; y++) {
                int loc = x + (y * pictureWidth);

                if (x >= borderX && x <= currentHSX && y >= borderY && y < currentHSY) {
                    final[loc] = original[loc];
                } else {
                    final[loc] = Color.white;
                }

            }
        }
        return final;


    }

    private bool isPixelWhite(Color c) {

        return c.r == 1 && c.g == 1 && c.b == 1;
    }




    /*---------------------------------------------
    /*---bake to File from Render and Webcam-------
    /*-------------------------------------------*/


    private void Update() {
        if (Input.GetKeyUp(KeyCode.Space)) {
            //  TakeScreenshot(pictureWidth, pictureHeight);
            performStuff();
            Debug.Log("space pressed");
        }
    }

    /*
        private void renderImage(Camera cam) {
            //  Debug.Log("post render");
            if (takePicture) {
                takePicture = false;
            /    RenderTexture renderTexture = camera.targetTexture;

                Texture2D renderResult = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
                Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
                renderResult.ReadPixels(rect, 0, 0);
                //hier für weiterverarbeiten abgreifen

                byte[] byteArray = renderResult.EncodeToPNG();
                string path = filePath + fileName; // path richtig setzen !
                System.IO.File.WriteAllBytes(Application.dataPath + "/" + fileName + ".png", byteArray);
                Debug.Log("Save to " + path);
                //cleanup
                RenderTexture.ReleaseTemporary(renderTexture);
                camera.targetTexture = null;
            }
        }
        private void TakeScreenshot(int width, int height) {
            camera.targetTexture = RenderTexture.GetTemporary(width, height);
            Debug.Log("take Screenshot initialized");
            takePicture = true;
        }

        */

    private Color[] BakeCameraPixelsOn2dTexture() {

        Color[] pixelArray = webcam.GetPixels();
        Texture2D texture = new(webcam.width, webcam.height); // safe name of constructor
        texture.SetPixels(pixelArray);
        byte[] byteArray = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + fileName + ".png", byteArray);
        Debug.LogFormat("image from Camera safed to " + Application.dataPath + "/" + fileName);
        return pixelArray;
    }
}

struct CorrectCoordinate
{
    public int x;
    public int y;
}