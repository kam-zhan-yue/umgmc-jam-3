using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class Sound
{
    public string id;
    public AudioClip clip;
    public bool loop = false;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(.1f, 3f)] public float pitch = 1f;
}