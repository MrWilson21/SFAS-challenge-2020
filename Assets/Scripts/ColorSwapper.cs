using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSwapper : MonoBehaviour
{
    [SerializeField] [Range(0, 1)] private float mixRatio = 0.5f;
    [SerializeField] private bool includeParent = true;

    private List<Material> materials;
    private List<Color> originalColours;

    void Awake()
    {
        materials = new List<Material>();
        originalColours = new List<Color>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if(!r.Equals(GetComponent<Renderer>()) || includeParent)
            {
                foreach (Material m in r.materials)
                {
                    materials.Add(m);
                    originalColours.Add(m.color);
                }
            }
        }
    }

    public void swapColour(Color colour)
    {
        for(int i = 0; i < materials.Count; i++)
        {
            materials[i].color = colour * mixRatio + originalColours[i] * (1 - mixRatio);
        }
    }

    public void restoreColour()
    {
        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].color = originalColours[i];
        }
    }
}
