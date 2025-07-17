using System.Collections.Generic;
using UnityEngine;

public class ButtonInfomation : MonoBehaviour
{
    public GameObject groupInfo;
    public List<GameObject> lstModel;

    private int count;
    private bool isOpen = false;

    // Store AudioSource of current model
    private AudioSource currentAudio;

    void Start()
    {
        groupInfo.SetActive(false);
        lstModel.ForEach(model => model.SetActive(false)); // Make sure all models are hidden at start
        if (lstModel.Count > 0)
        {
            lstModel[0].SetActive(true); // Show first model by default
            count = 0;
        }
    }

    public void OnClickButtonInfo()
    {
        isOpen = !isOpen;
        groupInfo.SetActive(isOpen);

        if (!isOpen && currentAudio != null && currentAudio.isPlaying)
        {
            currentAudio.Stop();
        }
    }

    public void OnClickRightButton()
    {
        SwitchModel(1);
    }

    public void OnClickLeftButton()
    {
        SwitchModel(-1);
    }

    void SwitchModel(int direction)
    {
        lstModel[count].SetActive(false);

        count += direction;
        if (count >= lstModel.Count) count = 0;
        if (count < 0) count = lstModel.Count - 1;

        lstModel[count].SetActive(true);

        // Stop audio from previous model if playing
        if (currentAudio != null && currentAudio.isPlaying)
        {
            currentAudio.Stop();
        }
    }

    public void OnClickSpeaker()
    {
        GameObject currentModel = lstModel[count];
        AudioSource audio = currentModel.GetComponent<AudioSource>();

        if (audio != null)
        {
            if (audio.isPlaying)
            {
                audio.Stop();
            }
            else
            {
                audio.Play();
                currentAudio = audio;
            }
        }
    }
}
