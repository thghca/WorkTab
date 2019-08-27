﻿using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using static WorkTab.InteractionUtilities;

namespace WorkTab
{
    public class PawnColumnWorker_WorkTabLabel : PawnColumnWorker_Label
    {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            // intercept interactions before base has a chance to act on them
            if ( Shift && Mouse.IsOver(rect))
            {
                if (ScrolledUp(rect))
                {
                    Increment(pawn);
                    return;
                }
                if (ScrolledDown(rect))
                {
                    Decrement(pawn);
                    return;
                }
            }

            // call base
            base.DoCell(rect, pawn, table);

            // override tooltip
            TooltipHandler.ClearTooltipsFrom(rect);
            TooltipHandler.TipRegion(rect, GetTooltip(pawn));
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return String.Compare( a.Name.ToStringShort, b.Name.ToStringShort, StringComparison.CurrentCultureIgnoreCase );
        }

        public string GetTooltip(Pawn pawn)
        {
            return "WorkTab.LabelCellTip".Translate() + "\n\n" + pawn.GetTooltip().text;
        }

        public void Decrement(Pawn pawn)
        {
            bool actionTaken = false;
            // just go over all workgivers and lower their priority number by one, with a minimum of 1
            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (pawn.story.WorkTypeIsDisabled(workgiver.workType))
                    continue;

                // get current priority
                var priority = pawn.GetPriority(workgiver, MainTabWindow_WorkTab.VisibleHour);

                // detailed mode
                if (PriorityManager.ShowPriorities)
                {
                    if (priority == 0)
                    {
                        priority = Settings.Get().maxPriority;
                        actionTaken = true;
                    }
                    else if (priority != 1)
                    {
                        priority--;
                        actionTaken = true;
                    }
                }
                else // simple mode
                {
                    if (priority == 0)
                        actionTaken = true;
                    priority = 3;
                }

                // apply new priority
                pawn.SetPriority(workgiver, priority, MainTabWindow_WorkTab.SelectedHours);
            }

            if (actionTaken && Settings.Get().playSounds)
                SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
        }

        public void Increment(Pawn pawn)
        {
            bool actionTaken = false;
            // just go over all workgivers and increase their priority number by one, with a maximum of maxPriority
            foreach (var workgiver in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (pawn.story.WorkTypeIsDisabled(workgiver.workType))
                    continue;

                // get current priority
                var priority = pawn.GetPriority(workgiver, MainTabWindow_WorkTab.VisibleHour);

                // detailed mode
                if (PriorityManager.ShowPriorities)
                {
                    if (priority == Settings.Get().maxPriority)
                    {
                        priority = 0;
                        actionTaken = true;
                    }
                    else if (priority != 0)
                    {
                        priority++;
                        actionTaken = true;
                    }
                }
                else // simple mode
                {
                    if (priority != 0)
                        actionTaken = true;
                    priority = 0;
                }


                // apply new priority
                pawn.SetPriority(workgiver, priority, MainTabWindow_WorkTab.SelectedHours);
            }

            if (actionTaken && Settings.Get().playSounds)
                SoundDefOf.AmountDecrement.PlayOneShotOnCamera();
        }
    }
}