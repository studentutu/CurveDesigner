﻿namespace ChaseMacMillan.CurveDesigner
{
    public interface ISamplerPoint
    {
        float Time { get; set; }
        int SegmentIndex { get; set; }
        void SetDistance(float distance,BezierCurve curve,bool shouldSort=true);
        KeyframeInterpolationMode InterpolationMode { get; set; } 
    }
}
