﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointAlongCurveComposite : IComposite, IPositionProvider
    {
        public ILinePoint value;
        private PointComposite _point;
        private BezierCurve _positionCurve;

        public PointAlongCurveComposite(IComposite parent,FloatDistanceValue value,Curve3D curve,Color color) : base(parent)
        {
            this.value = value;
            _point = new PointComposite(this, this, PointTextureType.square, new LinePointPositionClickCommand(value, curve),color);
            _positionCurve = curve.positionCurve;
        }

        public Vector3 Position {
            get
            {
                GetPositionForwardAndReference(out Vector3 position, out Vector3 forward,out Vector3 reference);
                return position;
            }
        }

        public void GetPositionForwardAndReference(out Vector3 position, out Vector3 forward, out Vector3 reference)
        {
            var point = _positionCurve.GetPointAtDistance(value.DistanceAlongCurve);
            position = point.position;
            forward = point.tangent;
            reference = point.reference;
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _point;
        }
    }
}