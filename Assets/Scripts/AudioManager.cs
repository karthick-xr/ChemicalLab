using System;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public Action<Vector3> SoundSpawn;
    public Action<Vector3> SoundCreated;
    public Action<Vector3> SoundSeperation;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource2D; // For UI/Global sounds
    [SerializeField] private GameObject sfx3DPrefab; // A prefab with an AudioSource for spatial sounds

    [Header("Audio Clips")]
    public AudioClip atomSpawnClip;
    public AudioClip moleculeCreatedClip;
    public AudioClip separationClip; // For when a molecule breaks apart

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Play a sound at a specific 3D location (e.g., the Reaction Plate)
    public void PlaySpatialSFX(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        // Create a temporary audio source at the position
        GameObject tempSource = Instantiate(sfx3DPrefab, position, Quaternion.identity);
        AudioSource source = tempSource.GetComponent<AudioSource>();

        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        // Destroy the object after the clip finishes
        Destroy(tempSource, clip.length);
    }

    // Helper methods for your specific events
    public void PlayAtomSpawn(Vector3 position) => PlaySpatialSFX(atomSpawnClip, position, 0.7f, UnityEngine.Random.Range(0.9f, 1.1f));

    public void PlayMoleculeCreated(Vector3 position) => PlaySpatialSFX(moleculeCreatedClip, position, 1f, 1f);

    public void PlaySeparation(Vector3 position) => PlaySpatialSFX(separationClip, position, 0.8f, 0.9f);
}