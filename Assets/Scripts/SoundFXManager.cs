using Unity.VisualScripting;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;
    [SerializeField] private AudioSource soundFXObject;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        if (audioClip == null || soundFXObject == null)
            return;

        Vector3 spawnPos = spawnTransform != null ? spawnTransform.position : transform.position;

        //spawn in gameObject
        AudioSource audioSource = Instantiate(soundFXObject, spawnPos, Quaternion.identity);

        //assign the audioClip
        audioSource.clip = audioClip;

        //assign volume
        audioSource.volume = volume;

        //play sound
        audioSource.Play();

        //get length of sound FX clip
        float clipLength = audioSource.clip.length;

        //destroy the clip after it is done playing
        Destroy(audioSource.gameObject, clipLength);
    }
    public void PlayRandomSoundFXClip(AudioClip[] audioClip, Transform spawnTransform, float volume)
    {
        if (audioClip == null || audioClip.Length == 0 || soundFXObject == null)
            return;

        //assign a random index
        int rand = Random.Range(0, audioClip.Length);
        if (audioClip[rand] == null)
            return;

        Vector3 spawnPos = spawnTransform != null ? spawnTransform.position : transform.position;

        //spawn in gameObject
        AudioSource audioSource = Instantiate(soundFXObject, spawnPos, Quaternion.identity);

        //assign the audioClip
        audioSource.clip = audioClip[rand];

        //assign volume
        audioSource.volume = volume;

        //play sound
        audioSource.Play();

        //get length of sound FX clip
        float clipLength = audioSource.clip.length;

        //destroy the clip after it is done playing
        Destroy(audioSource.gameObject, clipLength);
    }
}
