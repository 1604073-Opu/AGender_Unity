using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Text;
using TensorFlow;

public class CameraBehaviourScript : MonoBehaviour
{
    public Text label;
    int i = 0;
    private static bool camAvailable;
    private static WebCamTexture frontCam, backCam;
    public AspectRatioFitter fit;
    public RawImage background;
    int img_width = 32;
    int img_height = 32;
    private float[,,,] inputImg = new float[1, 32, 32, 1];
    public TextAsset age, gender;
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.Log("No camera availale");
            return;
        }
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                frontCam = new WebCamTexture(devices[i].name);
                break;
            }
        }
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                backCam = new WebCamTexture(devices[i].name);
                break;
            }
        }
        frontCam.Play();
        background.texture = frontCam;
        camAvailable = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!camAvailable)
        {
            return;
        }
        float ratio = (float)1.2;
        if (backCam != null && backCam.isPlaying)
        {
            ratio = (float)backCam.width / (float)backCam.height;
            fit.aspectRatio = ratio;
            float scaleY = backCam.videoVerticallyMirrored ? -1f : 1f;
            background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);
            int orient = -backCam.videoRotationAngle;
            background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
        }
        else
        {
            ratio = (float)frontCam.width / (float)frontCam.height;
            fit.aspectRatio = ratio;
            float scaleY = frontCam.videoVerticallyMirrored ? -1f : 1f;
            background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);
            int orient = -frontCam.videoRotationAngle;
            background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
        }
        Texture2D input = TextureToTexture2D(background.texture);
        input = ScaleTexture(input, 32, 32);
        if (backCam != null && backCam.isPlaying)
        {
            input = rotateTexture(input, true);
        }
        else
        {
            input = rotateTexture(input, false);
        }
        input = ConvertToGrayscale(input);
        byte[] bytes2 = input.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + "/Cropped" + i.ToString() + ".png", bytes2);
        //File.WriteAllBytes(Application.dataPath + "/Cropped"+i.ToString()+".png", bytes2);
        i = i + 1;
        Evaluate(input);
    }
    Texture2D rotateTexture(Texture2D originalTexture, bool clockwise)
    {
        Color32[] original = originalTexture.GetPixels32();
        Color32[] rotated = new Color32[original.Length];
        int w = originalTexture.width;
        int h = originalTexture.height;

        int iRotated, iOriginal;

        for (int j = 0; j < h; ++j)
        {
            for (int i = 0; i < w; ++i)
            {
                iRotated = (i + 1) * h - j - 1;
                iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                rotated[iRotated] = original[iOriginal];
            }
        }

        Texture2D rotatedTexture = new Texture2D(h, w);
        rotatedTexture.SetPixels32(rotated);
        rotatedTexture.Apply();
        return rotatedTexture;
    }

    public void switchCam()
    {

        if (backCam != null && backCam.isPlaying)
        {
            backCam.Pause();
            frontCam.Play();
            background.texture = frontCam;
        }
        else
        {
            frontCam.Pause();
            backCam.Play();
            background.texture = backCam;
        }
    }
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
        Color[] rpixels = result.GetPixels(0);
        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);
        for (int px = 0; px < rpixels.Length; px++)
        {
            rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
        }
        result.SetPixels(rpixels, 0);
        result.Apply();
        return result;
    }

    private Texture2D TextureToTexture2D(Texture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        Graphics.Blit(texture, renderTexture);

        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);

        int x = 275;
        int y = 170;
        int Xsize = 180;
        //int Xsize=120;
        int Ysize = 150;
        if (backCam != null && backCam.isPlaying)
        {
            x = 200;
            y = 170;
            Xsize = 180;
            Ysize = 125;
        }
        Texture2D texture2D1 = new Texture2D(Xsize, Ysize, TextureFormat.RGBA32, false);
        Color[] pixels = texture2D.GetPixels(x, y, Xsize, Ysize);
        texture2D1.SetPixels(pixels);

        return texture2D1;
    }

    private Texture2D ConvertToGrayscale(Texture2D graph)
    {
        Color32[] pixels = graph.GetPixels32();
        for (int x = 0; x < graph.width; x++)
        {
            for (int y = 0; y < graph.height; y++)
            {
                Color32 pixel = pixels[x + y * graph.width];
                int p = ((256 * 256 + pixel.r) * 256 + pixel.b) * 256 + pixel.g;
                int b = p % 256;
                p = Mathf.FloorToInt(p / 256);
                int g = p % 256;
                p = Mathf.FloorToInt(p / 256);
                int r = p % 256;
                float l = (0.2126f * r / 255f) + 0.7152f * (g / 255f) + 0.0722f * (b / 255f);
                Color c = new Color(l, l, l, 1);
                graph.SetPixel(x, y, c);
            }
        }
        graph.Apply();
        return graph;
    }

    private int getAge()
    {
        var graphA = new TFGraph();
        graphA.Import(age.bytes);
        var sessionA = new TFSession(graphA);
        var runnerA = sessionA.GetRunner();
        runnerA.AddInput(graphA["Input"][0], inputImg);
        runnerA.Fetch(graphA["pred_age/Softmax"][0]);
        float[,] recurrent_tensorA = runnerA.Run()[0].GetValue() as float[,];


        float highest_val = -100;
        int highest_ind = -1;

        for (int j = 0; j < 14; j++)
        {
            float confidence = recurrent_tensorA[0, j];
            if (highest_ind > -1)
            {
                if (confidence > highest_val)
                {
                    highest_val = confidence;
                    highest_ind = j;
                }
            }
            else
            {
                highest_val = confidence;
                highest_ind = j;
            }
        }
        Debug.Log("Age");
        Debug.Log(highest_val);
        return highest_ind;
    }

    private int getGender()
    {
        var graphG = new TFGraph();
        graphG.Import(gender.bytes);
        var sessionG = new TFSession(graphG);
        var runnerG = sessionG.GetRunner();
        runnerG.AddInput(graphG["Input"][0], inputImg);
        runnerG.Fetch(graphG["pred_gender/Softmax"][0]);
        float[,] recurrent_tensorG = runnerG.Run()[0].GetValue() as float[,];
        int highest_indG = -1;
        float highest_valG = -2;
        float f = recurrent_tensorG[0, 0];
        float m = recurrent_tensorG[0, 1];
        if (f > 0.43)
            highest_indG = 0;
        else highest_indG = 1;
        Debug.Log("Gender");
        Debug.Log(f);
        return highest_indG;
    }
    private void Evaluate(Texture2D input)
    {
        // Get raw pixel values from texture, format for inputImg array
        for (int i = 0; i < img_width; i++)
        {
            for (int j = 0; j < img_height; j++)
            {
                //inputImg[0, img_width - i - 1, j, 0] = input.GetPixel(j, i).r;
                inputImg[0, i, j, 0] = input.GetPixel(i, j).r;
            }
        }
        int highest_ind = getAge();
        int highest_indG = getGender();
        string gen;
        if (highest_indG == 0) gen = "Female";
        else gen = "Male";
        highest_ind = highest_ind * 5;
        label.text = highest_ind.ToString() + '-' + (highest_ind + 4).ToString() + " Y, " + gen;
    }
}
