using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AerowingController : MonoBehaviour
{
    [Header("Scale")]
    public float WORLD_SCALE = 0.01f;
    
    [Header("Input")]
    public bool invertX = false;
    public bool invertY = true;
    public float stickX;
    public float stickY;
    public bool LHeld, RHeld;
    public bool LPressed, RPressed;
    public bool boostHeld, brakeHeld;
    
    private InputActions.AerowingActions inputActions;
    
    [Header("Track Rails")]
    [Tooltip("The center of the track at the current z")]
    public float xPath, yPath;
    public float pathWidth = 700f;
    public float pathHeight = 680f;
    public float pathFloor;
    public float trackYawDeg;
    public float trackPitchDeg;
    public float groundHeight = 0f;
    
    [Header("Speed")]
    public float baseSpeed = 40f;
    public float boostSpeed;
    public float boostMeter;
    public bool boostCooldown;
    private const float BOOST_DEPLETE = 3.0f;
    private const float BOOST_RECOVER = 0.5f;

    [Header("Rotation")]
    public float rotY, rotX, rotZ;
    public float zRotBank;
    public float zRotBarrelRoll;
    public float aerobaticPitch = 0f;
    public float bankAngle;

    [Header("Barrel Roll")]
    public int rollState;
    public int rollTimer;
    public int rollRate;
    private int rollInputTimerL, rollInputTimerR;
    private float velocityMultiplier = 1f;
    
    [Header("Collision")]
    public Transform hitRightWing, hitLeftWing, hitTop, hitBottom;
    public float knockbackDistance = 20f;
    private Vector3 knockback;
    
    [Header("Health")]
    public int shields = 255;
    public int mercyTimer = 0;
    public bool rightWingBroken, leftWingBroken;
    public int rightWingHealth = 60, leftWingHealth = 60;
    
    [Header("Damage Shake")]
    public int hitTimer;
    public float damageShake;
    public float xShake;
    private int shakeSign = 1;
    
    [Header("Wing Effects")]
    public float rockPhase, bobPhase;
    public float xRock, yBob, rockAngle;
    public GameObject leftWingFixed, leftWingDamaged, rightWingFixed, rightWingDamaged;
    
    [Header("Output")]
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion shipRotation;

    private const float TURN_RATE = 2.3f;
    private const float TURN_STICK_MOD = 0.68f;
    
    [Header("References")]
    public GameObject visualMesh;
    public Camera viewCamera;
    public GameObject levelRoot;
    
    // HELPERS
    private float SmoothStepToF(ref float value, float target, float scale, float step, float minDiff)
    {
        float diff = target - value;
        if (Mathf.Abs(diff) <= minDiff)
        {
            value = target;
            return 0f;
        }
        float stepSize = diff * scale;
        stepSize = Mathf.Clamp(stepSize, -step, step);
        value += stepSize;
        return stepSize;
    }

    private float SmoothStepToAngle(ref float value, float target, float scale, float step, float minDiff)
    {
        float diff = Mathf.DeltaAngle(value, target);
        if (Mathf.Abs(diff) <= minDiff)
        {
            value = target;
            return 0f;
        }
        float stepSize = diff * scale;
        stepSize = Mathf.Clamp(stepSize, -step, step);
        value += stepSize;
        return stepSize;
    }
    
    // MAIN MOVEMENT
    private void Tick()
    {
        if (mercyTimer > 0) mercyTimer--;
        
        AerowingBank();
        AerowingBoost();
        AerowingBrake();
        UpdateAerowingRoll();
        MoveAerowingOnRails();
        UpdateDamageShake();
        UpdateWingEffects();
        
        position.x += knockback.x;
        position.y += knockback.y;
        SmoothStepToF(ref knockback.x, 0f, 0.1f, 1f, 0.5f);
        SmoothStepToF(ref knockback.y, 0f, 0.1f, 1f, 0.5f);
    }
    
    private void AerowingBank()
    {
        float target = 0f;
        float step = 0.1f;

        if (LHeld && !RHeld)
        {
            target = 90f;
            step = 0.2f;
            if (zRotBank < 70f && position.y < groundHeight + 70f)
            {
                position.y += 6f;
            }
        }

        if (RHeld && !LHeld)
        {
            target = -90f;
            step = 0.2f;
            if (zRotBank > -70f && position.y < groundHeight + 70f)
            {
                position.y += 6f;
            }
        }
        
        SmoothStepToF(ref zRotBank, target, step, 10f, 0f);

        if (LPressed)
        {
            if (rollInputTimerL != 0) StartRoll(30);
            else rollInputTimerL = 10;
        }

        if (RPressed)
        {
            if (rollInputTimerR != 0) StartRoll(-30);
            else rollInputTimerR = 10;
        }
    }

    private void StartRoll(int rate)
    {
        rollState = 1;
        rollTimer = 10;
        rollRate = rate;
    }
    
    private void UpdateAerowingRoll()
    {
        SmoothStepToF(ref velocityMultiplier, 1.0f, 0.05f, 10f, 0.0001f);
        zRotBarrelRoll %= 360f;

        if (rollState == 0)
            SmoothStepToF(ref zRotBarrelRoll, 0f, 0.1f, 10f, 0.00001f);

        if (rollInputTimerL != 0) rollInputTimerL--;
        if (rollInputTimerR != 0) rollInputTimerR--;
        if (rollTimer != 0) rollTimer--;

        if (rollState != 0)
        {
            rollInputTimerL = 0;
            rollInputTimerR = 0;
            velocityMultiplier = 1.5f;
            zRotBarrelRoll += rollRate;

            if (rollTimer == 0)
            {
                if (rollRate > 0) rollRate -= 5;
                if (rollRate < 0) rollRate += 5;
                if (rollRate == 0) rollState = 0;
            }
        }
    }
    
    private void AerowingBoost()
    {
        if (boostMeter != 0f && brakeHeld && boostHeld)
        {
            boostCooldown = true;
        }

        if (boostHeld && !brakeHeld && !boostCooldown)
        {
            boostMeter += BOOST_DEPLETE;
            if (boostMeter > 90f)
            {
                boostMeter = 90f;
                boostCooldown = true;
            }

            boostSpeed += 2.0f;
            if (boostSpeed > 30f)
            {
                boostSpeed = 30f;
            }
        }
        else
        {
            if (boostMeter > 0f)
            {
                boostMeter -= BOOST_RECOVER;
                if (boostMeter <= 0f)
                {
                    boostMeter = 0f;
                    boostCooldown = false;
                }
            }

            if (boostSpeed > 0f)
            {
                boostSpeed -= 1.0f;
                if (boostSpeed < 0f) boostSpeed = 0f;
            }
        }
    }
    
    private void AerowingBrake()
    {
        if (brakeHeld && !boostHeld && !boostCooldown)
        {
            boostMeter += BOOST_DEPLETE;
            if (boostMeter > 90f)
            {
                boostMeter = 90f;
                boostCooldown = true;
            }

            boostSpeed -= 1.0f;
            if (boostSpeed < -20f) boostSpeed = -20f;
        }
        else if (boostMeter > 0f)
        {
            boostMeter -= BOOST_RECOVER;
            if (boostMeter <= 0f)
            {
                boostMeter = 0f;
                boostCooldown = false;
            }

            if (boostSpeed < 0f)
            {
                boostSpeed += 0.5f;
                if (boostSpeed > 0f) boostSpeed = 0f;
            }
        }
    }
    
    private void MoveAerowingOnRails()
    {
        float turnRate = TURN_RATE;
        float sx = -stickX;
        float sy = stickY;

        SmoothStepToAngle(ref aerobaticPitch, 0f, 0.1f, 5f, 0.01f);
        
        // Yaw
        float yawScale = 0.1f;
        if ((zRotBank > 10f && sx > 0f) || (zRotBank < -10f && sx < 0f))
        {
            yawScale = 0.2f;
            turnRate *= 2f;
        }
        if (rollState != 0)
        {
            yawScale = 0.2f;
            turnRate = 6.9f;
        }
        SmoothStepToF(ref rotY, sx * TURN_STICK_MOD, yawScale, turnRate, 0.03f);

        // Pitch
        turnRate = TURN_RATE;
        float pitchScale = 0.1f;
        float pitchTarget = -sy * TURN_STICK_MOD;
        if (pitchTarget <= 0f && position.y < groundHeight + 50f)
        {
            pitchTarget = 0f;
            pitchScale = 0.2f;
            turnRate *= 2f;
        }
        SmoothStepToF(ref rotX, pitchTarget, pitchScale, turnRate, 0.03f);

        // Visual Roll
        float heightFactor;
        if (position.y < groundHeight + 70f)
        {
            heightFactor = 0.8f;
        }
        else
        {
            heightFactor = 1.0f;
        }
        if (!((LHeld && RHeld) || (!LHeld && !RHeld)))
        {
            heightFactor = 0.1f;
        }

        float rollStep;
        if (sx == 0f)
        {
            rollStep = 1f;
        }
        else
        {
            rollStep = 4f;
        }
        SmoothStepToF(ref rotZ, sx * 0.6f * heightFactor, 0.1f, rollStep, 0.03f);

        bankAngle = rotZ + zRotBank + zRotBarrelRoll;

        // Base Forward Vector
        Quaternion offsetRot = Quaternion.Euler(-(rotX + aerobaticPitch), rotY + 180f, 0f);
        Vector3 baseVel = offsetRot * new Vector3(0f, 0f, baseSpeed);
        baseVel.x *= 1.4f;
        baseVel.y *= 1.4f;

        // Tracking Head Reprojection
        Quaternion trackRot = Quaternion.Euler(trackPitchDeg, trackYawDeg, 0f);
        Vector3 forwardVel = trackRot * baseVel;
        Vector3 boostVel = trackRot * new Vector3(0f, 0f, -boostSpeed);
        Quaternion rollRot = Quaternion.Euler(0f, 0f, bankAngle);
        shipRotation = trackRot * offsetRot * rollRot;

        velocity.x = (forwardVel.x + boostVel.x) * velocityMultiplier;
        velocity.y = (forwardVel.y + boostVel.y) * velocityMultiplier;
        velocity.z = forwardVel.z + boostVel.z;

        position.x += velocity.x;
        if (position.x > xPath + pathWidth)
        {
            position.x = xPath + pathWidth; velocity.x = 0f;
        }
        if (position.x < xPath - pathWidth)
        {
            position.x = xPath - pathWidth; velocity.x = 0f;
        }

        position.y += velocity.y;
        if (position.y > yPath + pathHeight)
        {
            position.y = yPath + pathHeight; velocity.y = 0f;
        }
        if (position.y < yPath + pathFloor)
        {
            position.y = yPath + pathFloor; velocity.y = 0f;
        }

        position.z += velocity.z;
    }
    
    // COLLISION AND DAMAGE
    public void OnHitboxCollision(HitDirections hitDirection, ObstacleHitbox obstacle)
    {
        if (mercyTimer > 0 || obstacle.isWoosh) return;
        ApplyDamage(hitDirection, obstacle.damage);
    }

    private void ApplyDamage(HitDirections hitDirection, int damage)
    {
        shields -= damage;
        if (shields < 0) shields = 0;
        mercyTimer = 20;
        hitTimer = 20;
        shakeSign = GetShakeSign(hitDirection);

        Vector3 localKnock = Vector3.zero;
        switch (hitDirection)
        {
            case HitDirections.RightWing: localKnock = new Vector3(-knockbackDistance, 0f, 0f); DamageWing(false); break;
            case HitDirections.LeftWing: localKnock = new Vector3(knockbackDistance, 0f, 0f); DamageWing(true); break;
            case HitDirections.Top: localKnock = new Vector3(0f, -knockbackDistance, 0f); break;
            case HitDirections.Bottom: localKnock = new Vector3(0f, knockbackDistance, 0f); break;
        }

        Vector3 worldKnock = Quaternion.Euler(0f, rotY + 180f, 0f) * localKnock;
        knockback.x += worldKnock.x;
        knockback.y += worldKnock.y;
    }

    private void DamageWing(bool left)
    {
        if (left && !leftWingBroken)
        {
            leftWingHealth -= 20;
            if (leftWingHealth <= 0) leftWingBroken = true;
        }
        else if (!left && !rightWingBroken)
        {
            rightWingHealth -= 20;
            if (rightWingHealth <= 0) rightWingBroken = true;
        }
    }

    private void UpdateHitboxes()
    {
        Vector3 worldPos = transform.position;
        float rightX = rightWingBroken ? 30f : 40f;
        float leftX = leftWingBroken ? -30f : -40f;
        
        hitRightWing.position = worldPos + shipRotation * new Vector3(rightX, 0f, 0f) * WORLD_SCALE;
        hitLeftWing.position = worldPos + shipRotation * new Vector3(leftX, 0f, 0f) * WORLD_SCALE;
        hitTop.position = worldPos + shipRotation * new Vector3(0f, 24f, 0f) * WORLD_SCALE;
        hitBottom.position = worldPos + shipRotation * new Vector3(0f, -24f, 0f) * WORLD_SCALE;
    }

    private int GetShakeSign(HitDirections direction)
    {
        switch (direction)
        {
            case HitDirections.RightWing: return 1;
            case HitDirections.LeftWing: return -1;
            case HitDirections.Top: return 1;
            case HitDirections.Bottom: return -1;
            default: return 1;
        }
    }

    private void UpdateDamageShake()
    {
        if (hitTimer <= 0) return;

        hitTimer--;
        damageShake = Mathf.Sin(hitTimer * 400f * Mathf.Deg2Rad) * hitTimer * shakeSign;
        xShake = damageShake * 0.8f;

        if (hitTimer == 0)
        {
            damageShake = 0f;
            xShake = 0f;
        }
    }

    private void UpdateWingEffects()
    {
        xRock = Mathf.Sin(rockPhase * 0.7f * Mathf.Deg2Rad) * 0.5f;
        bobPhase += 10f;
        rockPhase += 8f;
        yBob = -Mathf.Sin(bobPhase * Mathf.Deg2Rad) * 0.5f;

        float amplitude = (rightWingBroken || leftWingBroken) ? 0.5f : 1.5f;
        rockAngle = Mathf.Sin(rockPhase * Mathf.Deg2Rad) * amplitude;
    }
    
    // MONO BEHAVIOUR LINKS
    private void Awake()
    {
        // Setup inputs
        inputActions = new InputActions().Aerowing;
        inputActions.Enable();
        
        pathFloor = groundHeight + 40f;
    }

    private void Update()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        LHeld = inputActions.LeftAction.IsPressed();
        RHeld = inputActions.RightAction.IsPressed();

        LPressed = inputActions.LeftAction.WasPressedThisFrame();
        RPressed = inputActions.RightAction.WasPressedThisFrame();
        
        boostHeld = inputActions.Boost.IsPressed();
        brakeHeld = inputActions.Brake.IsPressed();

        Vector2 stick = inputActions.Move.ReadValue<Vector2>();
        var finalStickX = invertX ? stick.x : -stick.x;
        var finalStickY = invertY ? stick.y : -stick.y;
        stickX = finalStickX;
        stickY = finalStickY;
    }

    private void FixedUpdate()
    {
        Tick();
        transform.position = new Vector3(position.x, position.y, 0) * WORLD_SCALE;
        Quaternion wobble = Quaternion.Euler(0f, 0f, rockAngle + xRock + damageShake * 1f);
        visualMesh.transform.rotation = shipRotation * wobble;
        UpdateHitboxes();

        // Wing broken visuals
        switch (leftWingBroken)
        {
            case true:
                leftWingFixed.SetActive(false);
                leftWingDamaged.SetActive(true);
                break;
            case false:
                leftWingFixed.SetActive(true);
                leftWingDamaged.SetActive(false);
                break;
        }
        switch (rightWingBroken)
        {
            case true:
                rightWingFixed.SetActive(false);
                rightWingDamaged.SetActive(true);
                break;
            case false:
                rightWingFixed.SetActive(true);
                rightWingDamaged.SetActive(false);
                break;
        }
    }
}
