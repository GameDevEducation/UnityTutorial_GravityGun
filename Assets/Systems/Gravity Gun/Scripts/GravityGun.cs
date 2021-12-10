using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GravityGun : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] Transform AnchorPoint;
    [SerializeField] LayerMask TargetMask = ~0;

    [Header("Grab")]
    [SerializeField] float MaxGrabTargetRange = 40f;
    [SerializeField] float GrabTime = 2f;
    [SerializeField] float GrabTorque = 10f;
    [SerializeField] Collider PlayerCollider;

    [Header("Push")]
    [SerializeField] TextMeshProUGUI ChargeDisplay;
    [SerializeField] float MinPushForce = 10f;
    [SerializeField] float MaxPushForce = 100f;
    [SerializeField] float PushChargeRate = 0.2f;
    [SerializeField] float MaxPushTargetRange = 30f;
    [SerializeField] ForceMode PushForceMode = ForceMode.Impulse;

    bool ChargingPush = false;
    bool PerformPush = false;
    float PushChargeProgress = 0f;

    Rigidbody GrabTarget;
    Collider GrabTargetCollider;
    float CurrentGrabTime;
    float GrabTargetRadius;

    Vector3 AnchorPosition => AnchorPoint.position + AnchorPoint.forward * GrabTargetRadius;

    // Start is called before the first frame update
    void Start()
    {
        ChargeDisplay.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        // charging?
        if (ChargingPush && PushChargeProgress < 1f)
        {
            PushChargeProgress = Mathf.Clamp01(PushChargeProgress + PushChargeRate * Time.deltaTime);
            ChargeDisplay.text = $"{Mathf.RoundToInt(PushChargeProgress * 100)}%";
        }
    }

    private void FixedUpdate()
    {
        // perform push?
        if (ChargingPush && PerformPush)
        {
            ChargingPush = PerformPush = false;
            ChargeDisplay.text = "";

            // attempt to find target
            Rigidbody targetRB = GrabTarget != null ? GrabTarget : FindTarget(true);
            if (targetRB != null)
            {
                if (GrabTarget != null)
                    ClearGrabTarget();

                float pushForce = Mathf.Lerp(MinPushForce, MaxPushForce, PushChargeProgress);
                targetRB.AddForce(Camera.main.transform.forward * pushForce, PushForceMode);
            }
        }
        else if (GrabTarget != null && CurrentGrabTime < GrabTime)
        {
            CurrentGrabTime += Time.deltaTime;

            // fully grabbed?
            if (CurrentGrabTime >= GrabTime)
            {
                GrabTarget.velocity = Vector3.zero;
                GrabTarget.transform.position = AnchorPosition;
                GrabTarget.isKinematic = true;
                GrabTarget.transform.SetParent(AnchorPoint);
            }
            else
            {
                float timeToAnchor = GrabTime - CurrentGrabTime;
                GrabTarget.velocity = (AnchorPosition - GrabTarget.transform.position) / timeToAnchor;
            }
        }
    }

    void ClearGrabTarget()
    {
        // de-parent if necessary
        if (GrabTarget.transform.parent == AnchorPoint)
            GrabTarget.transform.SetParent(null);

        GrabTarget.isKinematic = false;

        // re-enable collisions
        Physics.IgnoreCollision(GrabTargetCollider, PlayerCollider, false);

        // clear the target
        GrabTarget = null;
        GrabTargetCollider = null;
    }

    public void OnPrimary_Begin()
    {
        // attempt to find a target
        GrabTarget = FindTarget(false);
        if (GrabTarget != null)
        {
            // prepare for the grab
            GrabTargetCollider = GrabTarget.GetComponent<Collider>();
            GrabTargetRadius = GrabTargetCollider.bounds.extents.magnitude;
            CurrentGrabTime = 0f;

            if (GrabTorque != 0f)
                GrabTarget.AddTorque(Random.Range(-GrabTorque, GrabTorque),
                                     Random.Range(-GrabTorque, GrabTorque),
                                     Random.Range(-GrabTorque, GrabTorque));

            // turn off collisions with the player
            Physics.IgnoreCollision(GrabTargetCollider, PlayerCollider, true);
        }
    }

    public void OnPrimary_End()
    {
        if (GrabTarget != null)
            ClearGrabTarget();
    }

    public void OnSecondary_Begin()
    {
        ChargingPush = true;
        PushChargeProgress = 0f;
        PerformPush = false;
    }

    public void OnSecondary_End()
    {
        PerformPush = true;
    }

    Rigidbody FindTarget(bool isPush)
    {
        float searchRange = isPush ? MaxPushTargetRange : MaxGrabTargetRange;

        // raycast for the target
        RaycastHit hitResult;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
                            out hitResult, searchRange, TargetMask))
        {
            return hitResult.collider.GetComponent<Rigidbody>();
        }

        return null;
    }
}
