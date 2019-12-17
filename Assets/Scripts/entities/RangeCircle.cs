using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeCircle : MonoBehaviour
{
    [SerializeField]
    private int segments = 50;
    [SerializeField]
    public float radius = 5;
    LineRenderer line;

    private void Awake()
    {
        line = gameObject.GetComponent<LineRenderer>();
        line.positionCount = segments + 1;
        line.useWorldSpace = false;
        setRadius(radius);
    }

    public void showRadius()
    {
        line.enabled = true;
    }

    public void hideRadius()
    {
        line.enabled = false;
    }

    public void setRadius(float radius)
    {
        this.radius = radius / transform.lossyScale.x;
        line.enabled = true;
        CreatePoints();
    }

    void CreatePoints()
    {
        float x;
        float y;
        float z;

        float angle = 20f;

        for (int i = 0; i < (segments + 1); i++)
        {
            x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            line.SetPosition(i, new Vector3(x, 0, z));

            angle += (360f / segments + 1);
        }
    }
}
