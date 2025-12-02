using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class adjustVisuals : MonoBehaviour
{
    [SerializeField] private Volume volume;
    private VolumeProfile gameVolume;
    private ColorAdjustments colorAdjustments;
    private LiftGammaGain LiftGammaGain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameVolume = Instantiate(volume.profile);
        // Clone the specific component you want to modify
        if (gameVolume.TryGet(out colorAdjustments))
        {
            colorAdjustments.contrast.overrideState = true;
        }

        if (gameVolume.TryGet(out LiftGammaGain))
        {
            LiftGammaGain.gamma.overrideState = true;
        }
    }

    public void changeContrast(float contrast)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = contrast;
        }
    }

    public void changeGamma(float gamma)
    {
        if (LiftGammaGain != null)
        {
            LiftGammaGain.gamma.value = new Vector4(1, 1, 1, gamma);
        }
    }
}
