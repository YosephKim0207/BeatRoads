using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatMappingScript : MonoBehaviour
{
    AudioSource audioSource;
    float[] spectrums;
    Vector3 originScale;
    Vector3 newScale;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        spectrums = new float[1024];
        originScale = gameObject.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (audioSource)
        {
            audioSource.GetSpectrumData(spectrums, 0, FFTWindow.BlackmanHarris);
            newScale = originScale;
            newScale.y = spectrums[4];
            gameObject.transform.localScale = newScale;
        }
        else
        {
            Debug.Log($"{gameObject.name} : audio is NULL!");
        }
        
    }
}
