using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody Rigidbody;
    private Vector3 Direction;
    private DA.AI.DragonBehaviorYi_vally_shooting Mydragon;
    public int damage = 20;
    //private GameObject vally_target;

    void SelfDestruct()
    {
        Destroy(this.gameObject);
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Enemy")
        {
            //vally_target.transform.GetComponent<Enemy>().GetShot(damage);
            Mydragon.givereward(0.1f);
            Debug.Log("----------------------shooted--------------------");
            SelfDestruct();
        }
    }

    public void Init(Vector3 direction, DA.AI.DragonBehaviorYi_vally_shooting mydragon)
    {
        Direction = direction;
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.velocity = Direction * 30f;
        Mydragon = mydragon;
        //vally_target = GameObject.FindGameObjectWithTag("Enemy");
    }    


    void Start()
    {
        Invoke(nameof(SelfDestruct),0.5f);
    }
}
