using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSwapper : MonoBehaviour
{
    //Swaps color of object 

    [SerializeField] [Range(0, 1)] private float mixRatio = 0.5f; //Amount of new colour to take
    [SerializeField] private bool includeParent = true;  //Include top level object or not

    private List<Material> materials;
    private List<Color> originalColours; //Original colour saved so object can be restored

    void Awake()
    {
        //Get list of materials to change colour
        materials = new List<Material>();
        originalColours = new List<Color>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if(!r.Equals(GetComponent<Renderer>()) || includeParent)
            {
                foreach (Material m in r.materials)
                {
                    if(m.HasProperty(Shader.PropertyToID("_Color")))
                    {
                        materials.Add(m);
                        originalColours.Add(m.color);
                    }
                }
            }
        }
    }

    public void swapColour(Color colour)
    {
        //Swap colour of each material
        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].color = colour * mixRatio + originalColours[i] * (1 - mixRatio);
        }
    }

    public void restoreColour()
    {
        //Restore colour for each material
        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].color = originalColours[i];               
        }  
    }
}
