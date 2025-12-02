using UnityEngine;
using UnityEngine.UI;

public class Mute : MonoBehaviour
{
    public GameObject slider;
    private float volume = 0;

    public void muteToggle(bool mute)
    {
        if (mute)
        {
            volume = slider.GetComponent<Slider>().value;
            slider.GetComponent<Slider>().maxValue = 0;
        }
        else
        {
            slider.GetComponent<Slider>().maxValue = 1;
            slider.GetComponent<Slider>().value = volume;
        }
    }
        
}
