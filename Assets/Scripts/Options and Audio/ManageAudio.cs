using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class ManageAudio : MonoBehaviour
{
    public AudioMixer master;
    
    private void SetVolume(float volume, string source)
    {
        float volumeDB = (volume > 0) ? 20f * Mathf.Log10(volume) : -80f;
        master.SetFloat(source, volumeDB);

    }

    public void SetMusicVolume(float volume)
    {
        SetVolume(volume, "MusicVolume");
    }

    public void SetMasterVolume(float volume)
    {
        SetVolume(volume, "MasterVolume");
    }

    public void SetSEVolume(float volume)
    {
        SetVolume(volume, "SEVolume");
    }

}
