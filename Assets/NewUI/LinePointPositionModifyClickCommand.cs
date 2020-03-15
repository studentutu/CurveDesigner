﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class LinePointPositionClickCommand : IClickCommand
    {
        private ILinePoint _point;
        private Curve3D _curve;
        public LinePointPositionClickCommand(ILinePoint point,Curve3D curve)
        {
            _point = point;
            _curve = curve;
        }

        void SetPosition()
        {
            _point.DistanceAlongCurve = _curve.UICurve.pointClosestToCursor.distanceFromStartOfCurve;
        }

        public void ClickDown(Vector2 mousePos)
        {
            SetPosition();
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked)
        {
            SetPosition();
        }

        public void ClickUp(Vector2 mousePos)
        {
        }
    }
    public interface ILinePoint
    {
        float DistanceAlongCurve { get; set; } 
    }
}
