using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AerowingHitPoint : MonoBehaviour
{
    public AerowingController ship;
    public HitDirections hitDirection;

    private void OnTriggerStay(Collider other)
    {
        var obstacle = other.GetComponent<ObstacleHitbox>();
        if (obstacle == null) return;
        ship.OnHitboxCollision(hitDirection, obstacle);
    }
}

public enum HitDirections
{
    RightWing,
    LeftWing,
    Top,
    Bottom
}
