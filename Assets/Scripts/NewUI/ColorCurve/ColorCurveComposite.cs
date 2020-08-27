﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class ColorCurveComposite : IComposite, IWindowDrawer, IValueAlongCurvePointProvider
    {
        private ColorDistanceSampler sampler;
        private Curve3D curve;
        private List<EditColorComposite> colorPoints = new List<EditColorComposite>();
        private SplitterPointComposite splitterPoint;
        public ColorCurveComposite(IComposite parent,ColorDistanceSampler sampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            this.sampler = sampler;
            this.curve = curve;
            var pinkColor = new Color(.95f,.1f,.8f);
            splitterPoint = new SplitterPointComposite(this, new TransformBlob(curve.transform), PointTextureType.circle,new ValueAlongCurveSplitCommand(curve,sampler,ValueAlongCurveSplitCommand.GetColorCurve),pinkColor,positionCurveComposite);
            foreach (var i in sampler.GetPoints(curve.positionCurve))
                colorPoints.Add(new EditColorComposite(this,i,sampler,pinkColor,positionCurveComposite,curve));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(sampler.GetPoints(curve.positionCurve),curve);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return splitterPoint;
            foreach (var i in colorPoints)
                yield return i;
        }

        public IClickable GetPointAtIndex(int index)
        {
            return colorPoints[index].centerPoint.point;
        }
    }
}