﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class StatWorker_MoveSpeed : StatWorker
    {
        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanation(req, numberSense));
            if (req.HasThing)
            {
                CompInventory comp = req.Thing.TryGetComp<CompInventory>();
                if (comp != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("Carried weight: x" + comp.moveSpeedFactor.ToStringPercent());
                    if (comp.encumberPenalty > 0)
                    {
                        stringBuilder.AppendLine("Encumbered: -" + comp.encumberPenalty.ToStringPercent());
                        stringBuilder.AppendLine("Final modifier: x" + GetStatFactor(req.Thing).ToStringPercent());
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            float value = base.GetValueUnfinalized(req, applyPostProcess);
            if (req.HasThing)
            {
                value *= GetStatFactor(req.Thing);
            }
            return value;
        }

        private float GetStatFactor(Thing thing)
        {
            float factor = 1f;
            CompInventory comp = thing.TryGetComp<CompInventory>();
            if (comp != null)
            {
                factor = Mathf.Clamp(comp.moveSpeedFactor - comp.encumberPenalty, 0.01f, 1f);
            }
            return factor;
        }
    }
}