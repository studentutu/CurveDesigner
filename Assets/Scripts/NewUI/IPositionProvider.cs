﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IPositionProvider
    {
        Vector3 Position { get; }
    }
    public class PointGroupPointPositionProvider : IPositionProvider
    {
        private PointGroup _group;
        private PGIndex _type;
        private TransformBlob transformBlob;
        private BezierCurve _positionCurve;
        public PointGroupPointPositionProvider(PointGroup group,PGIndex type, TransformBlob transformBlob, BezierCurve positionCurve)
        {
            this.transformBlob = transformBlob;
            _group = group;
            _type = type;
            _positionCurve = positionCurve;
        }
        public Vector3 Position {
            get {
                return transformBlob.TransformPoint(_group.GetWorldPositionByIndex(_type,_positionCurve.dimensionLockMode));
            }
        }
    }
}
