﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class ProjectilePropertiesCR : ProjectileProperties
    {
        public float projectileArmorPenetration = 0;
        public int pelletCount = 1;
        public float spreadMult = 1;
    }
}