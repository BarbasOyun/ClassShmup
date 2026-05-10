using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Sound[] globalSounds;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject);
        }

        SetupSounds();
    }

    void Update()
    {

    }

    void SetupSounds()
    {
        for (int i = 0; i < globalSounds.Length; i++)
        {
            ref var s = ref globalSounds[i];

            if (s.source == null)
            {
                s.source = gameObject.AddComponent<AudioSource>();
            }

            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.playOnAwake = s.playOnAwake;
            s.source.loop = s.loop;

            if (s.playOnAwake)
            {
                s.source.Play();
            }
        }
    }

    public static void PlaySound(string name)
    {
        var sound = Array.Find(instance.globalSounds, s => s.name == name);

        if (sound.reset) sound.source.Stop();

        sound.source.Play();
    }

    // Better performance
    public static void PlaySound(int index)
    {
        var sound = instance.globalSounds[index];

        if (sound.reset) sound.source.Stop();

        sound.source.Play();
    }

    // StopSound
}

[Serializable]
public struct Sound
{
    public string name;
    public AudioClip clip;
    public AudioSource source;

    [Range(0f, 1f)]
    public float volume;
    // Bit masking
    public bool playOnAwake;
    public bool loop;
    public bool reset;
}