﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionCurveComposite : IComposite
    {
        public List<PositionPointGroupComposite> pointGroups = null;
        private SplitterPointComposite _splitterPoint = null;
        public BezierCurve positionCurve;
        private TransformBlob transformBlob;
        public PositionCurveComposite(IComposite parent,Curve3D curve,BezierCurve positionCurve,IClickCommand clickCommand, TransformBlob transformBlob) : base(parent)
        {
            this.transformBlob = transformBlob; 
            this.positionCurve = positionCurve;
            _splitterPoint = new SplitterPointComposite(this,curve,PointTextureType.circle,clickCommand,Curve3DSettings.Green);
            pointGroups = new List<PositionPointGroupComposite>();
            foreach (var group in positionCurve.PointGroups)
                pointGroups.Add(new PositionPointGroupComposite(this,group,transformBlob,positionCurve));
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in pointGroups)
                yield return i;
        }
    }
}
