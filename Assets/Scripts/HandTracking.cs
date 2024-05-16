using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class HandTracking : MonoBehaviour
{
    public DataRecieve dataReceiver;
    public GameObject[] lJointPoints;
    public GameObject[] rJointPoints;
    public GameObject globe;
    public GameObject globeRotator;

    private float[][] rLandmarkCoordinates;
    private float[][] lLandmarkCoordinates;
    private float[][] rLandmarkCoordinatesPrev;
    private float[][] lLandmarkCoordinatesPrev;

    public float minRotationSpeed = 1.0f;
    public float maxRotationSpeed = 10.0f;
    public float minScale = 3.0f;
    public float maxScale = 9.0f;
    private float pinchThreshold = 0.5f;
    private float swipeThreshold = 1.0f;
    private bool isPinching = false;

    private Vector3 newRotationGlobe;
    private float smoothingFactor = 0.4f;
    private float initialDistance;
    private Vector3 initialScale;

    private bool rightActive = false;
    private bool leftActive = false;

    private void Start()
    {
        rLandmarkCoordinates = new float[21][];
        lLandmarkCoordinates = new float[21][];
        rLandmarkCoordinatesPrev = new float[21][];
        lLandmarkCoordinatesPrev = new float[21][];

        for (int i = 0; i < rLandmarkCoordinates.Length; i++)
        {
            rLandmarkCoordinates[i] = new float[3];
            lLandmarkCoordinates[i] = new float[3];
            rLandmarkCoordinatesPrev[i] = new float[3];
            lLandmarkCoordinatesPrev[i] = new float[3];
        }
        SetDefault(lLandmarkCoordinates);
        SetDefault(rLandmarkCoordinates);

        newRotationGlobe = globe.transform.eulerAngles;
    }

    private void Update()
    {
        if (dataReceiver.rData.Length > 1)
        {
            ExtractLandmarkCoordinates(dataReceiver.rData, rLandmarkCoordinates);
        }

        if (dataReceiver.lData.Length > 1)
        {
            ExtractLandmarkCoordinates(dataReceiver.lData, lLandmarkCoordinates);
        }

        SmoothHandCoordinates(rJointPoints, rLandmarkCoordinates, "right");
        SmoothHandCoordinates(lJointPoints, lLandmarkCoordinates, "left");

        if (!(dataReceiver.rData.Length > 1))
        {
            SetDefault(rLandmarkCoordinates);

            for (int m = 0; m < 21; m++)
            {
                rJointPoints[m].transform.position = new Vector3(0f, 0f, 8.5f);
            }

            rightActive = false;
        }

        if (!(dataReceiver.lData.Length > 1))
        {
            SetDefault(lLandmarkCoordinates);

            for (int m = 0; m < 21; m++)
            {
                lJointPoints[m].transform.position = new Vector3(0f, 0f, 8.5f);
            }

            leftActive = false;
        }

        if (IsPinchGestureDetected(rLandmarkCoordinates[4], lLandmarkCoordinates[4], rLandmarkCoordinates[8], lLandmarkCoordinates[8]))
        {
            if (rightActive && leftActive)
            {
                HandlePinchGesture();
            }
        }
        else
        {
            isPinching = false;
        }

        if (IsSwipeGestureDetected(lLandmarkCoordinates[1], lLandmarkCoordinates[12]))
        {
            if (leftActive)
            {
                HandleSwipeGesture();
            }
        }
        else
        {
            globeRotator.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            globe.transform.eulerAngles = newRotationGlobe;
        }
    }

    private void ExtractLandmarkCoordinates(string data, float[][] landmarkCoordinates)
    {
        data = data.Remove(0, 1);
        data = data.Remove(data.Length - 1, 1);
        string[] handData = data.Split(',');

        for (int m = 0; m < 21; m++)
        {
            float x = ((float.Parse(handData[m * 3]) / 100) - 6);
            float y = ((float.Parse(handData[m * 3 + 1]) / 100) - 9);
            float z = ((float.Parse(handData[m * 3 + 2]) / 100) - 1);

            landmarkCoordinates[m][0] = x;
            landmarkCoordinates[m][1] = y;
            landmarkCoordinates[m][2] = z;
        }
    }

    private void SmoothHandCoordinates(GameObject[] jointPoints, float[][] landmarkCoordinates, string hand)
    {
        float[] fixedDistances = { 0.45f, 0.5f, 0.4f, 0.3f, 1.1f,
                           0.4f, 0.3f, 0.25f, 0.3f, 0.5f,
                           0.3f, 0.25f, 0.3f, 0.45f, 0.35f,
                           0.25f, 0.35f, 0.4f, 0.25f, 0.2f};

        int[,] specialDistancesIndices = { {0, 1}, {1, 2}, {2, 3}, {3, 4}, {0, 5}, {5, 6},
                               {6, 7}, {7, 8}, {5, 9}, {9, 10}, {10, 11},
                               {11, 12}, {9, 13}, {13, 14}, {14, 15}, {15, 16},
                               {13, 17}, {17, 18}, {18, 19}, {19, 20}};

        const float referenceZ = 2.5f;
        const float xAdjustmentFactor = 0.5f;

        Vector3 wristPointNew;
        float zDifference = landmarkCoordinates[0][2] - referenceZ;

        if (hand == "right")
        {
            wristPointNew = new Vector3(landmarkCoordinates[0][0] - zDifference * xAdjustmentFactor, landmarkCoordinates[0][1], landmarkCoordinates[0][2]);
            rightActive = true;
        }
        else
        {
            wristPointNew = new Vector3(landmarkCoordinates[0][0] + zDifference * xAdjustmentFactor, landmarkCoordinates[0][1], landmarkCoordinates[0][2]);
            leftActive = true;
        }
        jointPoints[0].transform.position = Vector3.Lerp(jointPoints[0].transform.position, wristPointNew, smoothingFactor);


        for (int i = 0; i < specialDistancesIndices.GetLength(0); i++)
        {
            int landmarkIndex1 = specialDistancesIndices[i, 0];
            int landmarkIndex2 = specialDistancesIndices[i, 1];

            float desiredDistance = fixedDistances[i];

            Vector3 objPrev = jointPoints[landmarkIndex1].transform.position;
            Vector3 landmarkPrev = new Vector3(landmarkCoordinates[landmarkIndex1][0], landmarkCoordinates[landmarkIndex1][1], landmarkCoordinates[landmarkIndex1][2]);
            Vector3 landmarkNext = new Vector3(landmarkCoordinates[landmarkIndex2][0], landmarkCoordinates[landmarkIndex2][1], landmarkCoordinates[landmarkIndex2][2]);

            Vector3 direction = (landmarkNext - landmarkPrev).normalized;

            Vector3 oldPostion = jointPoints[landmarkIndex2].transform.position;
            Vector3 newPosition = objPrev + direction * desiredDistance;

            jointPoints[landmarkIndex2].transform.position = Vector3.Lerp(oldPostion, newPosition, smoothingFactor);
        }
    }

    private void SetDefault(float[][] handlandmarkcoordinate)
    {
        for (int i = 0; i < handlandmarkcoordinate.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (j < 2)
                {
                    handlandmarkcoordinate[i][j] = 0f;
                }
                else
                {
                    handlandmarkcoordinate[i][j] = 8.5f;
                }
            }
        }
    }

    private bool IsPinchGestureDetected(float[] rThumbTip, float[] lThumbTip, float[] rIndexTip, float[] lIndexTip)
    {
        float distanceRight = Vector3.Distance(new Vector3(rThumbTip[0], rThumbTip[1], rThumbTip[2]),
                                               new Vector3(rIndexTip[0], rIndexTip[1], rIndexTip[2]));

        float distanceLeft = Vector3.Distance(new Vector3(lThumbTip[0], lThumbTip[1], lThumbTip[2]),
                                              new Vector3(lIndexTip[0], lIndexTip[1], lIndexTip[2]));

        return distanceRight < pinchThreshold && distanceLeft < pinchThreshold;
    }

    private bool IsSwipeGestureDetected(float[] lThumbBase, float[] lMiddleTip)
    {
        float distancePalm = Vector3.Distance(new Vector3(lThumbBase[0], lThumbBase[1], lThumbBase[2]),
                                               new Vector3(lMiddleTip[0], lMiddleTip[1], lMiddleTip[2]));

        return distancePalm < swipeThreshold;
    }

    private void HandlePinchGesture()
    {
        Vector3 rThumbTipPosition = new Vector3(rLandmarkCoordinates[4][0], rLandmarkCoordinates[4][1], rLandmarkCoordinates[4][2]);
        Vector3 lThumbTipPosition = new Vector3(lLandmarkCoordinates[4][0], lLandmarkCoordinates[4][1], lLandmarkCoordinates[4][2]);

        float currentDistance = Vector3.Distance(rThumbTipPosition, lThumbTipPosition);

        if (!isPinching)
        {
            initialDistance = currentDistance;
            initialScale = globe.transform.localScale;
            isPinching = true;
        }

        float scaleFactor = currentDistance / initialDistance;
        Vector3 targetScale = initialScale * scaleFactor;

        targetScale.x = Mathf.Clamp(targetScale.x, minScale, maxScale);
        targetScale.y = Mathf.Clamp(targetScale.y, minScale, maxScale);
        targetScale.z = Mathf.Clamp(targetScale.z, minScale, maxScale);

        if (!float.IsNaN(targetScale.x) && !float.IsNaN(targetScale.y) && !float.IsNaN(targetScale.z))
        {
            // Smoothly transition to the target scale using lerp
            globe.transform.localScale = Vector3.Lerp(globe.transform.localScale, targetScale, Time.deltaTime * 10.0f);
        }
    }

    private void HandleSwipeGesture()
    {
        Vector3 currentPosition = new Vector3(lLandmarkCoordinates[1][0], lLandmarkCoordinates[1][1], lLandmarkCoordinates[1][2]);
        Vector3 lookDirection = currentPosition - globeRotator.transform.position;

        if (lookDirection != Vector3.zero)
        {
            globeRotator.transform.rotation = Quaternion.LookRotation(-lookDirection, Vector3.up);
        }

        newRotationGlobe = globe.transform.eulerAngles;
    }
}
