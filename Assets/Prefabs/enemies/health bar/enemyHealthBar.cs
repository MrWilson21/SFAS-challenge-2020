using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class enemyHealthBar : MonoBehaviour
{
    Slider healthBar;

    public void hide()
    {
        healthBar.gameObject.SetActive(false);
    }

    public void show()
    {
        healthBar.gameObject.SetActive(true);
    }

    public void setValue(float value)
    {
        healthBar.value = value;
    }

    public void setMax(float maxValue)
    {
        healthBar.maxValue = maxValue;
    }

    void Awake()
    {
        healthBar = GetComponentInChildren<Slider>();
    }
}
