using UnityEngine;
using UnityEngine.UI;

public class MusicController : MonoBehaviour
{
    public AudioClip clickSound, cameraSound;

    public GameObject MusicSlider;
    public GameObject SoundSlider;

    public static MusicController USE;

    private AudioSource soundSource;

    private void Awake()
    {
        if (USE == null)
        {
            USE = this;
            DontDestroyOnLoad(gameObject);

            soundSource = transform.GetChild(0).GetComponent<AudioSource>();

            LoadSetting();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadSetting()
    {
        // Music
        AudioListener.volume = PlayerPrefs.GetInt("MusicSetting", 1);
    }

    public void ChangeMusicSetting()
    {
        AudioListener.volume = AudioListener.volume == 1 ? 0 : 1;

        PlayerPrefs.SetInt("MusicSetting", (int)AudioListener.volume);
        PlayerPrefs.Save();
    }

    public void PlaySound(AudioClip clip)
    {
        soundSource.PlayOneShot(clip);
    }

    public void onChangeMusic() {
        transform.GetComponent<AudioSource>().volume = MusicSlider.transform.GetComponent <Slider> ().value;
    }

    public void onChangedSound() {
        soundSource.volume = SoundSlider.transform.GetComponent <Slider> ().value;
    }


}