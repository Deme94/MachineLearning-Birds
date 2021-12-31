using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdRaceController : MonoBehaviour
{

    public int lap = 0;
    public int score = 0;
    public int position = 0;

    public Transform nextCheckpoint;
    public float distanceProgress = 0;

    public PlayerScoreView scoreView;

    public float CalculateDistanceProgress()
    {
        float distanceToCheckpoint = Vector3.Distance(transform.position, nextCheckpoint.position);
        float distanceProgressFromPreviousCheckpoint = nextCheckpoint.GetComponent<CheckpointController>().distance - distanceToCheckpoint; 
        return distanceProgress + distanceProgressFromPreviousCheckpoint;
    }

    public void SetPosition(int i)
    {
        if (position != i)
        {
            position = i;
            if(scoreView != null)
                scoreView.SetPosition(i);
        }
    }

    public void NextLap()
    {
        lap++;
        if (scoreView != null)
            scoreView.SetLap(lap);
    }

    public void SetNextCheckpoint(Transform checkpoint)
    {
        if (nextCheckpoint == null)
        {
            nextCheckpoint = checkpoint;
            nextCheckpoint.gameObject.SetActive(true);
        }
        else
        {
            distanceProgress += nextCheckpoint.GetComponent<CheckpointController>().distance;

            nextCheckpoint = checkpoint;
            GetComponent<BirdAgentController>().AddReward(100); // Optimo
            GetComponent<BirdAgentController>().SetNextCheckpoint(nextCheckpoint);
        }
    }
}
