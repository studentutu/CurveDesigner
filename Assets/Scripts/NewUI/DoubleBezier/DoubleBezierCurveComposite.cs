﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class DoubleBezierCurveComposite : IComposite, IValueAlongCurvePointProvider
    {
        private DoubleBezierSampler _doubleBezierSampler;
        private List<SecondaryPositionCurveComposite> _secondaryCurves;
        private SplitterPointComposite _splitterPoint;
        public DoubleBezierCurveComposite(IComposite parent,DoubleBezierSampler doubleBezierSampler,Curve3D curve) : base(parent)
        {
            _doubleBezierSampler = doubleBezierSampler;
            _secondaryCurves = new List<SecondaryPositionCurveComposite>();
            _splitterPoint = new SplitterPointComposite(this,curve, PointTextureType.circle, DoubleBezierCurveSplitCommandFactory.Instance, Color.green);
            foreach (var i in doubleBezierSampler.GetPoints(curve))
                _secondaryCurves.Add(new SecondaryPositionCurveComposite(this,curve,i));
        }
        public IClickable GetPointAtIndex(int index)
        {
            return _secondaryCurves[index].centerPoint.point;
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in _secondaryCurves)
                yield return i;
        }

    }
}
