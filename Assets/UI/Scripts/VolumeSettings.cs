using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
   [SerializeField] AudioMixer audioMixer;
   [SerializeField] Slider masterSlider;
   [SerializeField] Slider musicSlider;
   [SerializeField] Slider sfxSlider;

   void Start()
   {
      LoadVolume();
      SetSfxVolume();
      SetMasterVolume();
      SetMusicVolume();
   }

   public void SetMasterVolume()
   {
      float value = masterSlider.value;
      audioMixer.SetFloat("MasterVol", Mathf.Log10(value) * 20f);
      PlayerPrefs.SetFloat("MasterVol", value);
   }
   
   public void SetMusicVolume()
   {
      float value = musicSlider.value;
      audioMixer.SetFloat("MusicVol", Mathf.Log10(value) * 20f);
      PlayerPrefs.SetFloat("MusicVol", value);
   }
   
   public void SetSfxVolume()
   {
      float value = sfxSlider.value;
      audioMixer.SetFloat("SfxVol", Mathf.Log10(value) * 20f);
      PlayerPrefs.SetFloat("SfxVol", value);
   }

   public void LoadVolume()
   {
      if(PlayerPrefs.HasKey("MasterVol")) { masterSlider.value = PlayerPrefs.GetFloat("MasterVol");}
      if(PlayerPrefs.HasKey("MusicVol")) { musicSlider.value = PlayerPrefs.GetFloat("MusicVol");}
      if(PlayerPrefs.HasKey("SfxVol")) { sfxSlider.value = PlayerPrefs.GetFloat("SfxVol");}
   }
}
