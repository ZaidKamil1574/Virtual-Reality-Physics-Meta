using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class BoxControllerWithFriction : MonoBehaviour
{
    // ────────────── Designer Tweaks ──────────────
    [Header("References")]
    public Transform rightHandTransform;
    public Rigidbody boxRb;

    [Header("Pushing")]
    public float pushForce = 100f;           // N
    public float maxDistance = 5f;           // m
    public float maxPushAcceleration = 50f;  // m/s²

    [Header("Friction & Mass")]
    [Range(0, 1)] public float staticFrictionCoefficient  = .5f;
    [Range(0, 1)] public float kineticFrictionCoefficient = .3f;
    public float boxMass = 10f;
    public float gravity = 9.81f;

    [Header("Lock‑to‑box mode")]
    public float lockRadius = 2f;            // hand may drift this far
    public float runawayDamping = 5f;        // extra drag when locked (N·s/m)

    [Header("UI (optional)")]
    public Button lockToggleButton;          // assign the button here
    public TextMeshProUGUI lockStateLabel;   // optional label text
    /* … keep previous UI fields if you still want sliders … */

    // ────────────── private state ──────────────
    const float startRadiusFraction = .5f;
    float pushStartSqr, pushStopSqr;

    InputDevice leftHand, rightHand;
    Vector3 handPos;

    bool userWantsToPush;
    bool lockToBox;

    // ─────────────────────────────────────────────
    void Awake()
    {
        if (!boxRb) boxRb = GetComponent<Rigidbody>();
        boxRb.mass = boxMass;
        boxRb.interpolation = RigidbodyInterpolation.Interpolate;
        boxRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        leftHand  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        RecomputeRadius();

        if (lockToggleButton)
            lockToggleButton.onClick.AddListener(ToggleLockToBox);

        UpdateLockLabel();
        /* Init other UI (sliders) exactly like before … */
    }

    void RecomputeRadius()
    {
        float activeMax = lockToBox ? lockRadius : maxDistance;
        pushStartSqr = Mathf.Pow(activeMax * startRadiusFraction, 2);
        pushStopSqr  = activeMax * activeMax;
    }

    // ────────────── public UI hook ──────────────
    public void ToggleLockToBox()
    {
        lockToBox = !lockToBox;
        RecomputeRadius();
        UpdateLockLabel();
    }

    void UpdateLockLabel()
    {
        if (lockStateLabel)
            lockStateLabel.text = lockToBox ? "LOCK ON" : "LOCK OFF";
    }

    // ─────────────────────────────────────────────
    void Update()
    {
        if (rightHandTransform) handPos = rightHandTransform.position;

        // Inputs
        bool aPressed = false;
        rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out aPressed);

        Vector2 stick = Vector2.zero;
        leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);

        // Range hysteresis (uses lock radius when locked)
        float distSqr = (boxRb.position - handPos).sqrMagnitude;

        if (!userWantsToPush)
        {
            if (aPressed && stick.sqrMagnitude > 0.01f && distSqr < pushStartSqr)
                userWantsToPush = true;
        }
        else
        {
            if (!aPressed || (!lockToBox && distSqr > pushStopSqr))
                userWantsToPush = false;
        }

        /* velocity / friction UI display unchanged */
    }

    void FixedUpdate()
    {
        ApplyForces();
    }

    // ────────────── Physics core ──────────────
    void ApplyForces()
    {
        // 1. kinetic friction every step
        Vector3 v = boxRb.velocity;
        float normal = boxMass * gravity;

        if (v.sqrMagnitude > 0.0001f)
        {
            float fk = kineticFrictionCoefficient * normal;
            Vector3 fkAccel = -v.normalized * (fk / boxMass);
            boxRb.AddForce(fkAccel, ForceMode.Acceleration);
        }
        else
        {
            boxRb.velocity = Vector3.zero; // snap small drift
        }

        // 2. runaway damping if locked & box tries to escape
        if (lockToBox && !userWantsToPush)
        {
            // simple linear drag F = -c·v
            boxRb.AddForce(-v * runawayDamping, ForceMode.Force);
        }

        // 3. pushing force
        if (!userWantsToPush) return;

        Vector2 stick = Vector2.zero;
        leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);
        if (stick.sqrMagnitude < 0.01f) return;

        Vector3 dir = rightHandTransform.TransformDirection(new Vector3(stick.x, 0, stick.y).normalized);

        // static friction check
        if (boxRb.velocity.sqrMagnitude < 0.0001f)
        {
            float fs = staticFrictionCoefficient * normal;
            if (pushForce <= fs) return; // not enough to break static friction
        }

        float accelMag = Mathf.Clamp(pushForce / boxMass, 0f, maxPushAcceleration);
        boxRb.AddForce(dir * accelMag, ForceMode.Acceleration);
    }

    // ────────────── (reset / UI helper methods remain identical) ──────────────
}
