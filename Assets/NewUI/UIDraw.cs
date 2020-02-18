﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public enum PointTextureType {
        circle = 0,
        square = 1,
        diamond = 2,
    }
    public interface IDraw
    {
        void Draw();
        float DistFromCamera();
    } 
}
