﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Segment
{
    private const int _numSegmentLengthSamples = 10;
    public List<PointOnCurve> samples = new List<PointOnCurve>();
    public float length = 0;
    /// <summary>
    /// Cummulative length including current segment
    /// </summary>
    public float cummulativeLength=-1;
    public Segment(BeizerCurve owner,int segmentIndex)
    {
        Recalculate(owner,segmentIndex);
    }
    public void Recalculate(BeizerCurve owner,int segmentIndex)
    {
        samples.Clear();
        float len = 0;
        Vector3 previousPosition = owner.GetSegmentPositionAtTime(segmentIndex, 0.0f);
        AddLength(segmentIndex, 0.0f, 0, previousPosition);
        for (int i = 1; i <= _numSegmentLengthSamples; i++)//we include the end point with <=
        {
            var time = i / (float)_numSegmentLengthSamples;
            Vector3 currentPosition = owner.GetSegmentPositionAtTime(segmentIndex, time);
            var dist = Vector3.Distance(currentPosition, previousPosition);
            len += dist;
            AddLength(segmentIndex,time,len,currentPosition);
            previousPosition = currentPosition; 
        }
        this.length = len;
    }
    public Segment(Segment objToClone)
    {
        this.cummulativeLength = objToClone.cummulativeLength;
        this.length = objToClone.length;
        this.samples = new List<PointOnCurve>(objToClone.samples.Count);
        foreach (var i in objToClone.samples)
            samples.Add(new PointOnCurve(i));
    }
    public void AddLength(int segmentIndex,float time, float length, Vector3 position)
    {
        samples.Add(new PointOnCurve(time, length, position, segmentIndex));
    }
    public float GetTimeAtLength(float length)
    {
        if (length < 0 || length > this.length)
            throw new System.ArgumentException("Length out of bounds");
        PointOnCurve previousPoint = samples[0];
        if (previousPoint.distanceFromStartOfSegment > length)
            throw new System.Exception("Should always have a point at 0.0");
        for (int i = 1; i < samples.Count; i++)
        {
            var currentPoint = samples[i];
            if (currentPoint.distanceFromStartOfSegment > length)
            {
                float fullPieceLength = currentPoint.distanceFromStartOfSegment - previousPoint.distanceFromStartOfSegment;
                float partialPieceLength = length - previousPoint.distanceFromStartOfSegment;
                return Mathf.Lerp(previousPoint.time, currentPoint.time, partialPieceLength / fullPieceLength);
            }
            previousPoint = currentPoint;
        }
        return samples[samples.Count - 1].time;
    }
    public float GetDistanceAtTime(float time)
    {
        if (time < 0 || time > 1.0f)
            throw new System.ArgumentException("Length out of bounds");
        PointOnCurve previousPoint = samples[0];
        if (previousPoint.time > time)
            throw new System.Exception("Should always have a point at 0.0");
        for (int i = 1; i < samples.Count; i++)
        {
            var currentPoint = samples[i];
            if (currentPoint.time > time)
            {
                float fullPieceTime = currentPoint.time - previousPoint.time;
                float partialPieceTime = time - previousPoint.time;
                return Mathf.Lerp(previousPoint.distanceFromStartOfSegment, currentPoint.distanceFromStartOfSegment, partialPieceTime / fullPieceTime);
            }
            previousPoint = currentPoint;
        }
        return samples[samples.Count - 1].distanceFromStartOfSegment;
    }

}