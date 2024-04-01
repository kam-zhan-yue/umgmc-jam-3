using System;
using System.Collections;
using System.Collections.Generic;
using Kuroneko.UtilityDelivery;
using Sirenix.OdinInspector;
using TMPro.EditorUtilities;
using UnityEngine;
using Random = UnityEngine.Random;

public class Universe : MonoBehaviour, IUniverseService
{
    [BoxGroup("Scriptable Objects"), SerializeField] private PlanetDatabase planetDatabase;
    [NonSerialized, ShowInInspector, ReadOnly] private List<CelestialBody> _bodies = new();
    private GameObject _centre;
    private OrbitalSystem _universeOrbital;
    [SerializeField] private float angleStep = 10f;
    private int _ring = 3;
    
    private void Awake()
    {
        ServiceLocator.Instance.Register<IUniverseService>(this);
        _centre = UniverseHelper.GetCentre();
    }
    
    private void Start()
    {
        // InitPlanets();
    }

    private void InitPlanets()
    {
        for (int i = 0; i < 50; ++i)
        {
            PlanetData planetData = planetDatabase.GetRandomPlanet();
            ProcessPlanet(planetData);
        }
    }

    private void ProcessPlanet(PlanetData planetData)
    {
        CelestialBody body = Instantiate(planetData.prefab);
        body.RandomInit(10f, 100f);
        ProcessBody(body);
        if (planetData.moons)
        {
            int numMoons = Random.Range(0, 5);
            for (int i = 0; i < numMoons; ++i)
            {
                Moon moon = Instantiate(planetDatabase.moonPrefab, body.transform);
                moon.RandomInit(10f, 10f);
                ProcessBody(moon);
            }
        }
    }

    private Sun InstantiateSun()
    {
        return Instantiate(planetDatabase.sunPrefab);
    }

    private void ProcessBody(CelestialBody body)
    {
        _bodies.Add(body);
    }

    public Transform GetCentre()
    {
        return _centre.transform;
    }

    [Button]
    public void SpawnRing()
    {
        int fibonacci = UniverseHelper.GetFibonacci(_ring);
        float angle = angleStep * fibonacci;
        float scale = UniverseHelper.GetScaleModifier(fibonacci);
        planetDatabase.GeneratePlanet(fibonacci, angle, scale);
        _ring++;
    }

    [Button]
    public void DebugSpawnRing(int rings)
    {
        for (int i = 0; i < rings; ++i)
        {
            SpawnRing();
        }
    }

    private void OnDrawGizmosSelected()
    {
        for (int i = _ring; i < 100; ++i)
        {
            int fibonacci = UniverseHelper.GetFibonacci(i);
            float angle = angleStep * fibonacci;
            Vector2 rotation = UniverseHelper.ConvertAngleToRotation(angle);
            Gizmos.DrawSphere(rotation * fibonacci, UniverseHelper.GetScaleModifier(fibonacci));
            Gizmos.DrawWireSphere(transform.position, fibonacci);
        }
    }
}
