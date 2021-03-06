﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using CommunityCoreLibrary;


namespace Combat_Realism.Detours
{
    public static class Detour_AttackTargetFinder
    {
        internal static List<Thing> tmpTargets = new List<Thing>();

        internal static List<IntVec3> tempDestList = new List<IntVec3>();

        internal static List<IntVec3> tempSourceList = new List<IntVec3>();

        internal static Thing BestAttackTarget(Thing searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false)
        {
            Pawn searcherPawn = searcher as Pawn;
            Verb attackVerb = Detour_AttackTargetFinder.GetAttackVerb(searcher);
            if (attackVerb == null)
            {
                Log.Error("BestAttackTarget with " + searcher + " who has no attack verb.");
                return null;
            }
            bool onlyTargetMachines = attackVerb != null && attackVerb.verbProps.projectileDef != null && attackVerb.verbProps.projectileDef.projectile.damageDef == DamageDefOf.EMP;
            float minDistanceSquared = minDist * minDist;
            float num = maxTravelRadiusFromLocus + attackVerb.verbProps.range;
            float maxLocusDistSquared = num * num;
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (t == searcher)
                {
                    return false;
                }
                if (minDistanceSquared > 0f && (searcher.Position - t.Position).LengthHorizontalSquared < minDistanceSquared)
                {
                    return false;
                }
                if (maxTravelRadiusFromLocus < 9999f && (t.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
                {
                    return false;
                }
                if (!searcher.HostileTo(t))
                {
                    return false;
                }
                if (validator != null && !validator(t))
                {
                    return false;
                }
                if ((byte)(flags & TargetScanFlags.NeedLOSToAll) != 0 && !searcher.CanSee(t))
                {
                    if (t is Pawn)
                    {
                        if ((byte)(flags & TargetScanFlags.NeedLOSToPawns) != 0)
                        {
                            return false;
                        }
                    }
                    else if ((byte)(flags & TargetScanFlags.NeedLOSToNonPawns) != 0)
                    {
                        return false;
                    }
                }
                if ((byte)(flags & TargetScanFlags.NeedThreat) != 0 && ((IAttackTarget)t).ThreatDisabled())
                {
                    return false;
                }
                Pawn pawn = t as Pawn;
                if (onlyTargetMachines && pawn != null && pawn.RaceProps.IsFlesh)
                {
                    return false;
                }
                if ((byte)(flags & TargetScanFlags.NeedNonBurning) != 0 && t.IsBurning())
                {
                    return false;
                }
                if (searcher.def.race != null && searcher.def.race.intelligence >= Intelligence.Humanlike)
                {
                    CompExplosive compExplosive = t.TryGetComp<CompExplosive>();
                    if (compExplosive != null && compExplosive.wickStarted)
                    {
                        return false;
                    }
                }
                return true;
            };
            if (Detour_AttackTargetFinder.HasRangedAttack(searcher))
            {
                Detour_AttackTargetFinder.tmpTargets.Clear();
                List<IAttackTarget> potentialTargetsFor = Find.AttackTargetsCache.GetPotentialTargetsFor(searcher);
                for (int i = 0; i < potentialTargetsFor.Count; i++)
                {
                    Detour_AttackTargetFinder.tmpTargets.Add((Thing)potentialTargetsFor[i]);
                }
                if ((byte)(flags & TargetScanFlags.NeedReachable) != 0)
                {
                    Predicate<Thing> oldValidator = predicate;
                    predicate = delegate (Thing t)
                    {
                        if (!oldValidator(t))
                        {
                            return false;
                        }
                        if (searcherPawn != null)
                        {
                            if (!searcherPawn.CanReach(t, PathEndMode.Touch, Danger.Some, canBash, TraverseMode.ByPawn))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            TraverseMode mode = (!canBash) ? TraverseMode.NoPassClosedDoors : TraverseMode.PassDoors;
                            if (!searcher.Position.CanReach(t, PathEndMode.Touch, TraverseParms.For(mode, Danger.Deadly, false)))
                            {
                                return false;
                            }
                        }
                        return true;
                    };
                }
                Thing result = GenClosest.ClosestThing_Global(searcher.Position, Detour_AttackTargetFinder.tmpTargets, maxDist, predicate);
                Detour_AttackTargetFinder.tmpTargets.Clear();
                return result;

            }
                if (searcherPawn != null && searcherPawn.mindState.duty != null && searcherPawn.mindState.duty.radius > 0f)
                {
                    Predicate<Thing> oldValidator = predicate;
                    predicate = ((Thing t) => oldValidator(t) && t.Position.InHorDistOf(searcherPawn.mindState.duty.focus.Cell, searcherPawn.mindState.duty.radius));
                }
                int searchRegionsMax = (maxDist <= 800f) ? 40 : -1;
                IntVec3 arg_25D_0 = searcher.Position;
                ThingRequest arg_25D_1 = ThingRequest.ForGroup(ThingRequestGroup.AttackTarget);
                PathEndMode arg_25D_2 = PathEndMode.Touch;
                bool canBash2 = canBash;
                Thing thing = GenClosest.ClosestThingReachable(arg_25D_0, arg_25D_1, arg_25D_2, TraverseParms.For(searcherPawn, Danger.Deadly, TraverseMode.ByPawn, canBash2), maxDist, predicate, null, searchRegionsMax, false);
                if (thing != null && PawnUtility.ShouldCollideWithPawns(searcherPawn))
                {
                Thing thing2 = FindBestReachableMeleeTarget(predicate, searcherPawn, maxDist, canBash);
                    if (thing2 != null)
                    {
                        float lengthHorizontal = (searcherPawn.Position - thing.Position).LengthHorizontal;
                        float lengthHorizontal2 = (searcherPawn.Position - thing2.Position).LengthHorizontal;
                        if (Mathf.Abs(lengthHorizontal - lengthHorizontal2) < 50f)
                        {
                            thing = thing2;
                        }
                    }
                }
                return thing;
        }

        internal static Thing FindBestReachableMeleeTarget(Predicate<Thing> validator, Pawn searcherPawn, float maxTargDist, bool canBash)
        {
            maxTargDist = Mathf.Min(maxTargDist, 30f);
            Thing reachableTarget = null;
            Func<IntVec3, Thing> bestTargetOnCell = delegate (IntVec3 x)
            {
                List<Thing> thingList = x.GetThingList();
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    IAttackTarget attackTarget = thing as IAttackTarget;
                    if (attackTarget != null)
                    {
                        if (validator(thing))
                        {
                            if (searcherPawn.Position.IsAdjacentTo8WayOrInside(thing.Position, thing.Rotation, thing.def.size) || Find.AttackTargetReservations.CanReserve(searcherPawn, attackTarget))
                            {
                                return thing;
                            }
                        }
                    }
                }
                return null;
            };
            FloodFiller.FloodFill(searcherPawn.Position, delegate (IntVec3 x)
            {
                if (reachableTarget != null)
                {
                    return false;
                }
                if (!x.Walkable())
                {
                    return false;
                }
                if (x.DistanceToSquared(searcherPawn.Position) > maxTargDist * maxTargDist)
                {
                    return false;
                }
                if (!canBash)
                {
                    Building_Door building_Door = x.GetEdifice() as Building_Door;
                    if (building_Door != null && !building_Door.CanPhysicallyPass(searcherPawn))
                    {
                        return false;
                    }
                }
                return !PawnUtility.AnyPawnBlockingPathAt(x, searcherPawn, true, false);
            }, delegate (IntVec3 x)
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 intVec = x + GenAdj.AdjacentCells[i];
                    if (intVec.InBounds())
                    {
                        Thing thing = bestTargetOnCell(intVec);
                        if (thing != null)
                        {
                            reachableTarget = thing;
                            break;
                        }
                    }
                }
            });
            return reachableTarget;
        }

        internal static Verb GetAttackVerb(Thing attacker)
        {
            Pawn pawn = attacker as Pawn;
            if (pawn != null)
            {
                return pawn.TryGetAttackVerb(!pawn.IsColonist);
            }
            Building_Turret building_Turret = attacker as Building_Turret;
            if (building_Turret != null)
            {
                return building_Turret.AttackVerb;
            }
            return null;
        }

        internal static bool HasRangedAttack(Thing t)
        {
            if (t is Building_Turret)
            {
                return true;
            }
            Pawn pawn = t as Pawn;
            if (pawn == null)
            {
                Log.TryOpenLogWindow();
                Log.ErrorOnce("Unknown target searcher: " + t, 7547344);
                return true;
            }
            Thing thing = pawn.MannedThing();
            if (thing != null && thing is Building_Turret)
            {
                return true;
            }
            Verb verb = pawn.TryGetAttackVerb(true);
            return verb != null && !verb.verbProps.MeleeRange;
        }

        public static Thing BestShootTargetFromCurrentPosition(Thing searcher, Predicate<Thing> validator, float maxDistance, float minDistance, TargetScanFlags flags)
        {
            //if (searcher == null) return null;
            return AttackTargetFinder.BestAttackTarget(searcher, flags, validator, minDistance, maxDistance, default(IntVec3), 3.40282347E+38f, false);
        }

        public static bool CanSee(this Thing seer, Thing target)
        {
            ShootLeanUtility.CalcShootableCellsOf(Detour_AttackTargetFinder.tempDestList, target);
            for (int i = 0; i < Detour_AttackTargetFinder.tempDestList.Count; i++)
            {
                if (GenSight.LineOfSight(seer.Position, Detour_AttackTargetFinder.tempDestList[i], true))
                {
                    return true;
                }
            }
            ShootLeanUtility.LeanShootingSourcesFromTo(seer.Position, target.Position, Detour_AttackTargetFinder.tempSourceList);
            for (int j = 0; j < Detour_AttackTargetFinder.tempSourceList.Count; j++)
            {
                for (int k = 0; k < Detour_AttackTargetFinder.tempDestList.Count; k++)
                {
                    if (GenSight.LineOfSight(Detour_AttackTargetFinder.tempSourceList[j], Detour_AttackTargetFinder.tempDestList[k], true))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
