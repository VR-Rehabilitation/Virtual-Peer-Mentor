using Unity.XR.PXR;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TrackingManager : NetworkBehaviour
{
    [Header("Tracker Objects")]
    public GameObject leftFoot;
    public GameObject rightFoot;
    public GameObject waist;
    
    private Transform _objectTrackers;
    private bool _updateOt = true;

    void Start()
    {
        _objectTrackers = transform;
        
        // Check PICO Motion Tracking Support
        int res = PXR_MotionTracking.CheckMotionTrackerModeAndNumber(MotionTrackerMode.MotionTracking);
        if (res == 0) // 0 means Success
        {
            _objectTrackers.gameObject.SetActive(true);
            _updateOt = true;
        }
        else
        {
            Debug.LogError($"[TrackingManager] Motion tracking init failed with code: {res}");
        }
    }

    void Update()
    {
#if UNITY_ANDROID
        // Ensure we are in the correct mode
        MotionTrackerMode trackingMode = PXR_MotionTracking.GetMotionTrackerMode();
        if (trackingMode != MotionTrackerMode.MotionTracking)
        {
            // Attempt to re-initialize if mode is wrong
            PXR_MotionTracking.CheckMotionTrackerModeAndNumber(MotionTrackerMode.MotionTracking);
            return;
        }

        if (_updateOt && trackingMode == MotionTrackerMode.MotionTracking)
        {
            UpdateTrackerPositions();
        }
#endif
    }

    private void UpdateTrackerPositions()
    {
        MotionTrackerConnectState mtcs = new MotionTrackerConnectState();
        int ret = PXR_MotionTracking.GetMotionTrackerConnectStateWithSN(ref mtcs);

        if (ret != 0 || mtcs.trackerSum == 0) return;
        
        int foundTrackers = 0;
        
        // Loop through the fixed size array provided by SDK (usually 24 max capacity)
        for (int i = 0; i < mtcs.trackersSN.Length; i++)
        {
            // Stop if we have found 3 trackers (Feet + Waist)
            if (foundTrackers >= 3) break;

            string sn = mtcs.trackersSN[i].value.ToString().Trim();
            if (string.IsNullOrEmpty(sn)) continue;

            MotionTrackerLocations locations = new MotionTrackerLocations();
            MotionTrackerConfidence confidence = new MotionTrackerConfidence();
            
            // Get data for this specific tracker SN
            int result = PXR_MotionTracking.GetMotionTrackerLocations(
                mtcs.trackersSN[i], 
                ref locations, 
                ref confidence
            );

            if (result == 0)
            {
                MotionTrackerLocation localLocation = locations.localLocation;
                // Use ToVector3() extension from PICO SDK
                Vector3 position = localLocation.pose.Position.ToVector3();
                Quaternion rotation = localLocation.pose.Orientation.ToQuat();

                switch (foundTrackers)
                {
                    case 0: // Left Foot
                        if (leftFoot != null) UpdateObjectTransform(leftFoot, position, rotation);
                        break;
                    case 1: // Right Foot
                        if (rightFoot != null) UpdateObjectTransform(rightFoot, position, rotation);
                        break;
                    case 2: // Waist
                        if (waist != null) UpdateObjectTransform(waist, position, rotation); // Removed *5f scale as it seemed odd, usually units are meters.
                        break;
                }
                
                foundTrackers++;
            }
        }
    }

    private void UpdateObjectTransform(GameObject obj, Vector3 position, Quaternion rotation)
    {
        obj.transform.position = position;
        obj.transform.rotation = rotation;
    }

    [ServerRpc]
    private void UpdateTransformServerRpc(ulong networkObjectId, Vector3 position, Quaternion rotation)
    {
        UpdateTransformClientRpc(networkObjectId, position, rotation);
    }

    [ClientRpc]
    private void UpdateTransformClientRpc(ulong networkObjectId, Vector3 position, Quaternion rotation)
    {
        if (IsOwner) return; 

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
        }
    }
}
