﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [SerializeField]
    private int maxHealth = 100;

    private int currentHealth;

    public event Action<float> OnHealthPcChanged = delegate { };

    private void OnEnable() 
    {

        currentHealth = maxHealth;
      
    }

    public void ModifyHealth(int amount) 
    {
        currentHealth += amount;
        float currentHealthPct = (float)currentHealth / (float)maxHealth;
        OnHealthPcChanged(currentHealthPct);
    }

    private void update()
    {
        //ponemos la condicion para q se le descuente vida
        //llamamos a ModifyHealth(-10);  o al receive damage
    }
    
}
