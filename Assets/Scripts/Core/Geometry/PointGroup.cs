﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class CurveSplitPointInfo : ISegmentTime
    {
        public int segmentIndex;
        public float time;
        public CurveSplitPointInfo(int segmentIndex, float time)
        {
            this.segmentIndex = segmentIndex;
            this.time = time;
        }

        public int SegmentIndex { get { return segmentIndex; } }

        public float Time { get { return time; } }
    }
    /// <summary>l
    /// A point group groups 3 control points together, left tangent, right tangent and the point itself
    /// </summary>
    [System.Serializable]
    public class PointGroup : ISelectEditable<PointGroup>
    {
        #region fields
        [HideInInspector]
        [SerializeField]
        private bool isPointLocked = false;
        /// <summary>
        /// The right tangent in space relative to central point
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private Vector3 leftTangent;
        /// <summary>
        /// The left tangent in space relative to central point
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private Vector3 rightTangent;
        [HideInInspector]
        [SerializeField]
        private Vector3 position;
        [HideInInspector]
        [SerializeField]
        private SelectableGUID guid;
        [HideInInspector]
        [SerializeField]
        public int segmentIndex;

        [NonSerialized]
        public BezierCurve owner;

        public SelectableGUID GUID => guid;
        #endregion
        public PointGroup(bool lockState, Curve3D curve, BezierCurve owner)
        {
            SetPointLocked(lockState);
            this.guid = curve.guidFactory.GetGUID(this);
            this.owner = owner;
        }
        public PointGroup(PointGroup clone, Curve3D curve, BezierCurve newCurve, bool createNewGuids)
        {
            this.isPointLocked = clone.isPointLocked;
            this.leftTangent = clone.leftTangent;
            this.rightTangent = clone.rightTangent;
            this.position = clone.position;
            this.owner = newCurve;
            if (createNewGuids)
                this.guid = curve.guidFactory.GetGUID(this);
            else
                this.guid = clone.guid;
        }

        public bool DoesEditAffectBothSegments(PointGroupIndex index)
        {
            return true;
        }

        #region Get/Set methods
        public bool GetIsPointLocked()
        {
            return isPointLocked;
        }
        private Vector3 reflectAcrossPosition(Vector3 vect)
        {
            return -vect;
        }
        public void SetPointLocked(bool state)
        {
            isPointLocked = state;
            if (state == true)
            {
                rightTangent = reflectAcrossPosition(leftTangent);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LockAxis(Vector3 vector, DimensionLockMode lockMode)
        {
            switch (lockMode)
            {
                case DimensionLockMode.none:
                    return vector;
                case DimensionLockMode.x:
                    return new Vector3(0, vector.y, vector.z);
                case DimensionLockMode.y:
                    return new Vector3(vector.x, 0, vector.z);
                case DimensionLockMode.z:
                    return new Vector3(vector.x, vector.y, 0);
                default:
                    return Vector3.zero;
            }
        }
        public void SetWorldPositionByIndex(PointGroupIndex index, Vector3 value)
        {
            var dimensionLockMode = owner.dimensionLockMode;
            switch (index)
            {
                case PointGroupIndex.LeftTangent:
                    leftTangent = LockAxis(value, dimensionLockMode) - GetWorldPositionByIndex(PointGroupIndex.Position);
                    if (isPointLocked)
                        rightTangent = reflectAcrossPosition(leftTangent);
                    return;
                case PointGroupIndex.Position:
                    position = LockAxis(value, dimensionLockMode);
                    return;
                case PointGroupIndex.RightTangent:
                    rightTangent = LockAxis(value, dimensionLockMode) - GetWorldPositionByIndex(PointGroupIndex.Position);
                    if (isPointLocked)
                        leftTangent = reflectAcrossPosition(rightTangent);
                    return;
                default:
                    throw new System.ArgumentException();
            }
        }
        public Vector3 GetWorldPositionByIndex(PointGroupIndex index, bool reflect = false)
        {
            var dimensionLockMode = owner.dimensionLockMode;
            switch (index)
            {
                case PointGroupIndex.LeftTangent:
                    return LockAxis((reflect ? reflectAcrossPosition(leftTangent) : leftTangent) + position, dimensionLockMode);
                case PointGroupIndex.Position:
                    return LockAxis(position, dimensionLockMode);
                case PointGroupIndex.RightTangent:
                    return LockAxis((reflect ? reflectAcrossPosition(rightTangent) : rightTangent) + position, dimensionLockMode);
                default:
                    throw new System.ArgumentException();
            }
        }

        public float GetDistance(BezierCurve positionCurve)
        {
            return positionCurve.GetDistanceAtSegmentIndexAndTime(positionCurve.PointGroups.IndexOf(this), 0);
        }

        public void SelectEdit(Curve3D curve, List<PointGroup> selectedPoints)
        {
            var initialLocked = isPointLocked;
            bool? isLocked = null;
            float initialWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 370;
            bool currentLockState = EditorGUILayout.Toggle("Tangents Locked", initialLocked);
            EditorGUIUtility.labelWidth = initialWidth;
            if (initialLocked != currentLockState)
                isLocked = currentLockState;

            var initialLeft = GetWorldPositionByIndex(PointGroupIndex.LeftTangent);
            var initialPos = GetWorldPositionByIndex(PointGroupIndex.Position);
            var initialRight = GetWorldPositionByIndex(PointGroupIndex.RightTangent);

            var leftTangentOffset = EditorGUILayout.Vector3Field("Left Tangent", initialLeft - initialPos) - initialLeft + initialPos;
            var positionOffset = EditorGUILayout.Vector3Field("Position", initialPos) - initialPos;
            var rightTangentOffset = EditorGUILayout.Vector3Field("Right Tangent", initialRight - initialPos) - initialRight + initialPos;

            EditorGUIUtility.SetWantsMouseJumping(1);

            if (isLocked == initialLocked && initialLeft == leftTangentOffset && initialPos == positionOffset && initialRight == rightTangentOffset)
                return;

            foreach (var target in selectedPoints)
            {
                if (isLocked.HasValue)
                    target.SetPointLocked(isLocked.Value);
                target.SetWorldPositionByIndex(PointGroupIndex.Position, target.GetWorldPositionByIndex(PointGroupIndex.Position) + positionOffset);
                target.SetWorldPositionByIndex(PointGroupIndex.LeftTangent, target.GetWorldPositionByIndex(PointGroupIndex.LeftTangent) + leftTangentOffset);
                target.SetWorldPositionByIndex(PointGroupIndex.RightTangent, target.GetWorldPositionByIndex(PointGroupIndex.RightTangent) + rightTangentOffset);
            }
        }

        //point groups are always inside the visible curve
        public bool IsInsideVisibleCurve(BezierCurve curve)
        {
            return true;
        }
        #endregion
    }

}