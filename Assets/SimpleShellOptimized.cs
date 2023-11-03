using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleShellOptimized : MonoBehaviour
{
    [SerializeField] private Mesh shellMesh;
    [SerializeField] private Shader shellShader;
    [SerializeField] private Material shellMaterialAsset;
    [SerializeField] private bool useMaterialAsset;

    [SerializeField] private bool updateStatics = true;

    // These variables and what they do are explained on the shader code side of things
    // You can see below (line 70) which shader uniforms match up with these variables
    [Range(1, 256)]
    [SerializeField] private int shellCount = 16;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float shellLength = 0.15f;
    [Range(0.01f, 3.0f)]
    [SerializeField] private float distanceAttenuation = 1.0f;
    [Range(1.0f, 1000.0f)]
    [SerializeField] private float density = 100.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float noiseMin = 0.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float noiseMax = 1.0f;
    [Range(0.0f, 10.0f)]
    [SerializeField] private float thickness = 1.0f;
    [Range(0.0f, 10.0f)]
    [SerializeField] private float curvature = 1.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float displacementStrength = 0.1f;
    [SerializeField] private Color shellColor;
    [Range(0.0f, 5.0f)]
    [SerializeField] private float occlusionAttenuation = 1.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float occlusionBias = 0.0f;

    private Material shellMaterial;
    private GameObject[] shells;
    private MeshFilter[] shellMeshFilters;
    private MeshRenderer[] shellRenderers;
    private MaterialPropertyBlock[] shellMaterialPropertyBlocks;

    private Vector3 displacementDirection = new Vector3(0, 0, 0);

    void OnEnable()
    {
        if(useMaterialAsset)
            shellMaterial = shellMaterialAsset;
        else
            shellMaterial = new Material(shellShader);

        shellMaterial.enableInstancing = true;

        shells = new GameObject[shellCount];
        shellRenderers = new MeshRenderer[shellCount];
        shellMeshFilters = new MeshFilter[shellCount];
        shellMaterialPropertyBlocks = new MaterialPropertyBlock[shellCount];

        for (int i = 0; i < shellCount; ++i)
        {
            var shell = new GameObject("Shell " + i.ToString());
            shells[i] = shell;
            var meshFilter = shells[i].AddComponent<MeshFilter>();
            shellMeshFilters[i] = meshFilter;
            var meshRenderer = shells[i].AddComponent<MeshRenderer>();
            shellRenderers[i] = meshRenderer;

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            meshFilter.mesh = shellMesh;
            meshRenderer.sharedMaterial = shellMaterial;
            propertyBlock.SetInteger("_ShellIndex", i);
            meshRenderer.SetPropertyBlock(propertyBlock);
            shell.transform.SetParent(this.transform, false);
            shellMaterialPropertyBlocks[i] = propertyBlock;

        }

        // In order to tell the GPU what its uniform variable values should be, we use these "Set" functions which will set the
        // values over on the GPU. 
        shellMaterial.SetInt("_ShellCount", shellCount);
        shellMaterial.SetFloat("_ShellLength", shellLength);
        shellMaterial.SetFloat("_Density", density);
        shellMaterial.SetFloat("_Thickness", thickness);
        shellMaterial.SetFloat("_Attenuation", occlusionAttenuation);
        shellMaterial.SetFloat("_ShellDistanceAttenuation", distanceAttenuation);
        shellMaterial.SetFloat("_Curvature", curvature);
        shellMaterial.SetFloat("_DisplacementStrength", displacementStrength);
        shellMaterial.SetFloat("_OcclusionBias", occlusionBias);
        shellMaterial.SetFloat("_NoiseMin", noiseMin);
        shellMaterial.SetFloat("_NoiseMax", noiseMax);
        shellMaterial.SetVector("_ShellColor", shellColor);
    }

    void Update()
    {
        float velocity = 1.0f;

        Vector3 direction = new Vector3(0, 0, 0);

        // This determines the direction we are moving from wasd input. It's probably a better idea to use Unity's input system, since it handles
        // all possible input devices at once, but I did it the old fashioned way for simplicity.
        direction.x = Convert.ToInt16(Input.GetKey(KeyCode.D)) - Convert.ToInt16(Input.GetKey(KeyCode.A));
        direction.y = Convert.ToInt16(Input.GetKey(KeyCode.W)) - Convert.ToInt16(Input.GetKey(KeyCode.S));
        direction.z = Convert.ToInt16(Input.GetKey(KeyCode.Q)) - Convert.ToInt16(Input.GetKey(KeyCode.E));

        // This moves the ball according the input direction
        Vector3 currentPosition = this.transform.position;
        direction.Normalize();
        currentPosition += Time.deltaTime * velocity * direction;
        this.transform.position = currentPosition;

        // This changes the direction that the hair is going to point in, when we are not inputting any movements then we subtract the gravity vector
        // The gravity vector just being (0, -1, 0)
        displacementDirection -= 10.0f * Time.deltaTime * direction;
        if (direction == Vector3.zero)
            displacementDirection.y -= 10.0f * Time.deltaTime;

        if (displacementDirection.magnitude > 1) displacementDirection.Normalize();

        // In order to avoid setting this variable on every single shell's material instance, we instead set this is as a global shader variable
        // That every shader will have access to, which sounds bad, because it kind of is, but just be aware of your global variable names and it's not a big deal.
        // Regardless, setting the variable one time instead of 256 times is just better.
        Shader.SetGlobalVector("_ShellDirection", displacementDirection);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || !shellMaterial)
            return;

        if (updateStatics)
        {
            for (int i = 0; i < shellCount; ++i)
            {
                shellMaterialPropertyBlocks[i].SetInteger("_ShellIndex", i);
                shellRenderers[i].SetPropertyBlock(shellMaterialPropertyBlocks[i]);
            }
            shellMaterial.SetInt("_ShellCount", shellCount);
            shellMaterial.SetFloat("_ShellLength", shellLength);
            shellMaterial.SetFloat("_Density", density);
            shellMaterial.SetFloat("_Thickness", thickness);
            shellMaterial.SetFloat("_Attenuation", occlusionAttenuation);
            shellMaterial.SetFloat("_ShellDistanceAttenuation", distanceAttenuation);
            shellMaterial.SetFloat("_Curvature", curvature);
            shellMaterial.SetFloat("_DisplacementStrength", displacementStrength);
            shellMaterial.SetFloat("_OcclusionBias", occlusionBias);
            shellMaterial.SetFloat("_NoiseMin", noiseMin);
            shellMaterial.SetFloat("_NoiseMax", noiseMax);
            shellMaterial.SetVector("_ShellColor", shellColor);
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < shells.Length; ++i)
        {
            Destroy(shells[i]);
        }

        shells = null;
        shellMeshFilters = null;
        shellRenderers = null;
    }
}
