using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Scripts.Game.Managers
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
        [Serializable]
        public struct SoundData
        {
            public string name;
            public AudioClip clip;
        }

        [Header("SFX List")]
        public List<SoundData> sounds = new();
        public AudioSource audioSource;

        public void Play(string name, float volumeScale)
        {
            foreach (var sound in sounds.Where(sound => sound.name == name))
            {
                audioSource.PlayOneShot(sound.clip, volumeScale);
                return;
            }
        }

        public void PlayLoop(string name, float volume)
        {
            if (sounds.Any(sound => sound.name == name))
            {
                audioSource.clip = sounds.FirstOrDefault(sound => sound.name == name).clip;
                audioSource.volume = volume;
                audioSource.loop = true;
                audioSource.Play();
                return;
            }
        }

        public void StopLoop() => audioSource.Stop();

    }
}