﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    //should be using dependency injection for this, but with all these generics that'd be a total nightmare because the injected objects wouldn't serialize unless wrapped in yet another class lol
    [System.Serializable]
    public abstract class FieldEditableSamplerPoint<T,S,Q> : SamplerPoint<T,S,Q> where Q : DistanceSampler<T,S,Q> where S : FieldEditableSamplerPoint<T, S, Q>, new()
    {
        public abstract T Field(string displayName, T originalValue);
        public abstract T Subtract(T v1, T v2);
        public abstract T Add(T v1, T v2);
        public abstract T Zero();
        public abstract T MinChange(T v1, T v2);
        public abstract T MaxValue();
        public override void SelectEdit(Curve3D curve, List<S> selectedPoints)
        {
            T originalValue = value;
            T fieldVal = Field(owner.GetLabel(), originalValue);
            T valueOffset = Subtract(fieldVal,originalValue);
            T minChange = MaxValue();
            foreach (var i in selectedPoints)
            {
                T newVal = owner.Constrain(Add(i.value, valueOffset));
                T change = Subtract(newVal, i.value);
                minChange = MinChange(change,minChange);
            }
            base.SelectEdit(curve, selectedPoints);
            if (minChange.Equals(Zero()))
                return;
            foreach (var target in selectedPoints)
                target.value = Add(target.value,minChange);
        }
    }

    [System.Serializable]
    public abstract class SamplerPoint<T,S,Q> : ISelectEditable<S>, ISamplerPoint where Q : DistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public T value;
        [NonSerialized]
        public Q owner;
        public int segmentIndex;
        public float time;

        [SerializeField]
        private KeyframeInterpolationMode _interpolationMode = KeyframeInterpolationMode.Linear;
        public KeyframeInterpolationMode InterpolationMode { get => _interpolationMode; set => _interpolationMode= value; }

        public abstract T CloneValue(T value,bool createNewGuids);

        public void Copy(SamplerPoint<T,S,Q> objToClone, DistanceSampler<T,S,Q> newOwner,bool createNewGuids)
        {
            value = CloneValue(objToClone.value,createNewGuids);
            owner = newOwner as Q;
            segmentIndex = objToClone.segmentIndex;
            time = objToClone.time;
            _interpolationMode =  objToClone._interpolationMode;
        }

        [SerializeField]
        private SelectableGUID _guid;
        public SelectableGUID GUID { get { return _guid; } set { _guid = value; } }

        public float Time { get => time; set => time = value; }
        public int SegmentIndex { get => segmentIndex; set => segmentIndex = value; }

        public bool IsInsideVisibleCurve(BezierCurve curve)
        {
            return SegmentIndex < curve.NumSegments;
        }

        public virtual void SelectEdit(Curve3D curve, List<S> selectedPoints)
        {
            float originalDistance = GetDistance(curve.positionCurve);
            float distanceOffset = EditorGUILayout.FloatField("Distance", originalDistance) - originalDistance;
            KeyframeInterpolationMode newInterpolation = (KeyframeInterpolationMode)EditorGUILayout.EnumPopup("Interpolation",InterpolationMode);
            if (newInterpolation != InterpolationMode)
                foreach (var i in selectedPoints)
                    i.InterpolationMode = newInterpolation;
            if (distanceOffset == 0)
                return;
            EditorGUIUtility.SetWantsMouseJumping(1);
            PointOnCurveClickCommand.ClampOffset(distanceOffset, curve,selectedPoints);
        }

        public void SetDistance(float distance,BezierCurve curve, bool shouldSort = true)
        {
            var point = curve.GetPointAtDistance(distance);
            segmentIndex = point.segmentIndex;
            time = point.time;
            if (shouldSort)
                owner.Sort(curve);
        }

        public float GetDistance(BezierCurve positionCurve)
        {
            return positionCurve.GetDistanceAtSegmentIndexAndTime(segmentIndex,time);
        }
    }

    [System.Serializable]
    public abstract class ValueDistanceSampler<T,S,Q> : DistanceSampler<T, S, Q>, IValueSampler<T> where Q : ValueDistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public T constValue;

        [SerializeField]
        private bool _useKeyframes;

        public bool UseKeyframes { get => _useKeyframes; set => _useKeyframes = value; }
        public T ConstValue { get => constValue; set => constValue = value; }

        protected abstract T CloneValue(T value);

        public abstract T Lerp(T val1, T val2, float lerp);
        public ValueDistanceSampler(string label,Curve3DEditMode editMode) : base(label,editMode)
        {
        }
        public ValueDistanceSampler(ValueDistanceSampler<T,S,Q> objToClone,bool createNewGuids) : base(objToClone,createNewGuids) {
            _useKeyframes = objToClone._useKeyframes;
            constValue = CloneValue(objToClone.constValue);
        }
        protected override T GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            return GetValueAtDistance(distance,curve);
        }
        public T GetValueAtDistance(float distance, BezierCurve curve)
        {
            float curveLength = curve.GetLength();
            bool isClosedLoop = curve.isClosedLoop;
            var pointsInsideCurve = GetPoints(curve);
            if (!_useKeyframes || pointsInsideCurve.Count == 0)
            {
                return constValue;
            }
            var firstPoint = pointsInsideCurve[0];
            var lastPoint = pointsInsideCurve[pointsInsideCurve.Count - 1];
            var lastDistance = curveLength - lastPoint.GetDistance(curve);
            float endSegmentDistance = firstPoint.GetDistance(curve) + lastDistance;
            if (pointsInsideCurve[0].GetDistance(curve) >= distance)
            {
                if (isClosedLoop && lastPoint.InterpolationMode == KeyframeInterpolationMode.Linear)
                {
                    float lerpVal = (lastDistance + distance) / endSegmentDistance;
                    return Lerp(lastPoint.value, firstPoint.value, lerpVal);
                }
                else
                    return pointsInsideCurve[0].value;
            }
            var previous = pointsInsideCurve[0];
            for (int i = 1; i < pointsInsideCurve.Count; i++)
            {
                var current = pointsInsideCurve[i];
                if (current.GetDistance(curve) >= distance)
                {
                    if (previous.InterpolationMode == KeyframeInterpolationMode.Linear)
                        return Lerp(previous.value, current.value, (distance - previous.GetDistance(curve)) / (current.GetDistance(curve) - previous.GetDistance(curve)));
                    else
                        return previous.value;
                }
                previous = current;
            }
            if (isClosedLoop && lastPoint.InterpolationMode == KeyframeInterpolationMode.Linear)
            {
                float lerpVal = (distance - lastPoint.GetDistance(curve)) / endSegmentDistance;
                return Lerp(lastPoint.value, firstPoint.value, lerpVal);
            }
            else
                return pointsInsideCurve[pointsInsideCurve.Count - 1].value;
        }
    }

    [System.Serializable]
    public abstract class DistanceSampler<T,S,Q> : ISerializationCallbackReceiver, ISampler where Q : DistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public List<S> points = new List<S>();

        [NonSerialized]
        private List<S> points_openCurveOnly = null;

        public string fieldDisplayName="";

        [SerializeField]
        private string label;
        [SerializeField]
        private Curve3DEditMode editMode;
        public DistanceSampler(string label, Curve3DEditMode editMode) {
            this.label = label;
            this.editMode = editMode;
        }
        public virtual T Constrain(T v1)
        {
            return v1;
        }

        public DistanceSampler(DistanceSampler<T,S,Q> objToClone,bool createNewGuids)
        {
            S Clone(S obj)
            {
                var clonedPoint = new S();
                clonedPoint.Copy(obj, this,createNewGuids);
                return clonedPoint;
            }

            this.label = objToClone.label;
            this.editMode = objToClone.editMode;

            foreach (var i in objToClone.points)
                points.Add(Clone(i));

            points_openCurveOnly = new List<S>();

            foreach (var i in objToClone.points_openCurveOnly)
                points_openCurveOnly.Add(Clone(i));
        }
        public string GetLabel()
        {
            return label;
        }

        public Curve3DEditMode GetEditMode()
        {
            return editMode;
        }

        public IEnumerable<ISamplerPoint> AllPoints()
        {
            return points;
        }

        IEnumerable<ISamplerPoint> ISampler.GetPoints(BezierCurve curve)
        {
            return points;
        }

        protected abstract T GetInterpolatedValueAtDistance(float distance, BezierCurve curve);

        public int InsertPointAtDistance(float distance, BezierCurve curve) {
            T interpolatedValue = GetInterpolatedValueAtDistance(distance, curve);
            var newPoint = new S();
            newPoint.GUID = curve.owner.guidFactory.GetGUID(newPoint);
            newPoint.value = interpolatedValue;
            newPoint.owner = this as Q;
            var valuePoint = newPoint as ISamplerPoint;
            if (valuePoint != null && TryGetPointBelowDistance(distance, curve, out S point))
                valuePoint.InterpolationMode = point.InterpolationMode;
            points.Add(newPoint);
            newPoint.SetDistance(distance,curve);
            return points.IndexOf(newPoint);
        }

        private bool TryGetPointBelowDistance(float distance, BezierCurve curve,out S point)
        {
            point = null;
            var points = GetPoints(curve);
            if (points.Count == 0)
                return false;
            if (distance < points[0].GetDistance(curve)){
                if (curve.isClosedLoop)
                    point = points[points.Count - 1];
                else
                    point = points[0];
                return true;
            }
            for (int i = 0; i < points.Count; i++)
            {
                var curr = points[i];
                if (curr.GetDistance(curve) > distance)
                {
                    point = points[i - 1];
                    return true;
                }
            }
            point = points.Last();
            return true;
        }
        public List<S> GetPoints(BezierCurve curve)
        {
            if (curve.isClosedLoop)
                return points;
            if (points_openCurveOnly == null)
                RecalculateOpenCurveOnlyPoints(curve);
            return points_openCurveOnly;
        }

        /// <summary>
        /// Should be called whenever this sampler is sorted, when a point is deleted, when a point in this sampler is moved/inserted (which should trigger a sort), or after deserialization
        /// </summary>
        public void RecalculateOpenCurveOnlyPoints(BezierCurve curve)
        {
            points_openCurveOnly = new List<S>();
            foreach (var i in points)
                if (i.segmentIndex < curve.PointGroups.Count-1)
                    points_openCurveOnly.Add(i);
        }

        public void Sort(BezierCurve curve)
        {
            points = points.OrderBy((a) => a.time).OrderBy(a=>a.segmentIndex).ToList();
            RecalculateOpenCurveOnlyPoints(curve);
        }

        public void OnBeforeSerialize() { /*Do Nothing*/ }

        public void OnAfterDeserialize()
        {
            foreach (var i in points)
                i.owner = this as Q;
        }

        public virtual ISelectable GetSelectable(int index, Curve3D curve)
        {
            return GetPoints(curve.positionCurve)[index];
        }

        public virtual int NumSelectables(Curve3D curve)
        {
            return GetPoints(curve.positionCurve).Count;
        }

        public virtual bool Delete(List<SelectableGUID> guids, Curve3D curve)
        {
            bool retr = SelectableGUID.Delete(ref points, guids, curve);
            if (retr)
                RecalculateOpenCurveOnlyPoints(curve.positionCurve);
            return retr;
        }

        public virtual List<SelectableGUID> SelectAll(Curve3D curve)
        {
            List<SelectableGUID> retr = new List<SelectableGUID>();
            var points = GetPoints(curve.positionCurve);
            foreach (var i in points)
                retr.Add(i.GUID);
            return retr;
        }

        public string GetPointName()
        {
            return label.ToLower();
        }

        public abstract void ConstantField(Rect rect);
    }
}