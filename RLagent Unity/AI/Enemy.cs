using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public int startingHealth = 100;
    private int CurrentHealth;
    private Vector3 StartPosition;
    public bool status;

    
    public void Start()
    {
        StartPosition = transform.position;
        CurrentHealth = startingHealth;
        status = true;
    }


    public void GetShot(int damage)
    {
        ApplyDamage(damage);
    }
    
    private void ApplyDamage(int damage)
    {
        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        status = false;
        //Destroy(gameObject);
        this.gameObject.SetActive(false);
    }
    
}
