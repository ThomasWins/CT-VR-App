using UnityEngine;

public class VoiceTrigger : MonoBehaviour
{
    [Tooltip("Audio source that will play the voice line.")]
    public AudioSource voiceAudio;
    public AudioClip voiceClip;
    public GameObject trigger1;
    public GameObject trigger2;
    public bool mostRecent { get; set; } = false;

    private void OnEnable()
    {
        mostRecent = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !mostRecent)
        {
               voiceAudio.clip = voiceClip;
               voiceAudio.Play();
               mostRecent = true;
               trigger1.gameObject.GetComponent<VoiceTrigger>().mostRecent = false;
               trigger2.gameObject.GetComponent<VoiceTrigger>().mostRecent = false;
        }
    }
}
