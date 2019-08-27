// Karel Kroeze
// Pawn_Extensions.cs
// 2017-05-22

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using static WorkTab.TimeUtilities;

namespace WorkTab
{
    public static class Pawn_Extensions
    {
        public static int GetMinPriority(this Pawn pawn, WorkTypeDef worktype, int hour)
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            // get priorities for all workgivers in worktype
            var priorities = worktype.WorkGivers()
                .Select( wg => GetPriority( pawn, wg, hour ) )
                .Where( p => p > 0 );

            // if there are no active priorities, return zero
            if ( !priorities.Any() )
                return 0;

            // otherwise, return the lowest number (highest priority).
            if (Find.PlaySettings.useWorkPriorities)
                return priorities.Min();

            // or, in simple mode, just 3.
            return 3;
        }
        public static int GetMaxPriority(this Pawn pawn, WorkTypeDef worktype, int hour)
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            // get priorities for all workgivers in worktype
            var priorities = worktype.WorkGivers().Select( wg => GetPriority( pawn, wg, hour ) ).Where( p => p > 0 );

            // if there are no active priorities, return zero
            if ( !priorities.Any() )
                return 0;

            // otherwise, return the highest number (lowest priority).
            if (Find.PlaySettings.useWorkPriorities)
                return priorities.Max();

            // or, in simple mode, just 3.
            return 3;
        }
        public static bool AnyGiverMissingPriority(this Pawn pawn, WorkTypeDef worktype, int hour)
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            // get priorities for all workgivers in worktype
            return worktype.WorkGivers().Any(wg => GetPriority(pawn, wg, hour) == 0);
        }
        public static int GetPriority( this Pawn pawn, WorkTypeDef worktype, int hour )
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            return PriorityManager.Get[pawn].GetPriority( worktype, hour );
        }

        public static int[] GetPriorities(this Pawn pawn, WorkTypeDef worktype)
        {
            return PriorityManager.Get[pawn].GetPriorities( worktype );
        }

        public static int GetPriority(this Pawn pawn, WorkGiverDef workgiver, int hour )
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            return PriorityManager.Get[pawn].GetPriority( workgiver, hour );
        }

        public static int[] GetPriorities(this Pawn pawn, WorkGiverDef workgiver)
        {
            return PriorityManager.Get[pawn][workgiver].Priorities;
        }

        public static void SetPriority( this Pawn pawn, WorkTypeDef worktype, int priority, List<int> hours )
        {
            PriorityManager.Get[pawn].SetPriority( worktype, priority, hours );
        }

        public static void SetPriority(Pawn pawn, WorkTypeDef worktype, int priority, int hour, bool recache = true )
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            PriorityManager.Get[pawn].SetPriority( worktype, priority, hour, recache );
        }

        public static void SetPriority( this Pawn pawn, WorkGiverDef workgiver, int priority, List<int> hours )
        {
            PriorityManager.Get[pawn].SetPriority( workgiver, priority, hours );
        }

        public static void SetPriority( this Pawn pawn, WorkGiverDef workgiver, int priority, int hour, bool recache = true )
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            Logger.Trace($"Setting {pawn.LabelShort}'s {workgiver.defName} priority for {hour} to {priority}");

            PriorityManager.Get[pawn].SetPriority( workgiver, priority, hour, recache );
        }

        public static void DisableAll( this Pawn pawn )
        {
            foreach ( var worktype in DefDatabase<WorkTypeDef>.AllDefsListForReading )
                pawn.SetPriority( worktype, 0, null );
        }

        public static void ChangePriority( this Pawn pawn, WorkTypeDef worktype, int diff, List<int> hours )
        {
            foreach (int hour in (hours ?? WholeDay))
                ChangePriority(pawn, worktype, diff, hour, false);

            PriorityManager.Get[pawn].Recache(worktype);
        }

        public static void ChangePriority(Pawn pawn, WorkTypeDef worktype, int diff, int hour, bool recache = true )
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            foreach (WorkGiverDef workgiver in worktype.WorkGivers())
                ChangePriority(pawn, workgiver, diff, hour, false);

            if (recache)
                PriorityManager.Get[pawn].Recache(worktype);
        }

        public static void ChangePriority( this Pawn pawn, WorkGiverDef workgiver, int diff, int hour, bool recache = true )
        {
            if (hour < 0)
                hour = GenLocalDate.HourOfDay(pawn);

            int priority = pawn.GetPriority(workgiver, hour) + diff;
            SetPriority(pawn, workgiver, priority, hour, recache);
        }

        public static bool CapableOf( this Pawn pawn, WorkGiverDef workgiver )
        {
            return !workgiver.requiredCapacities.Any( c => !pawn.health.capacities.CapableOf( c ) );
        }

        public static bool AllowedToDo( this Pawn pawn, WorkGiverDef workgiver )
        {
            if ( pawn?.story == null )
                return true;
            return !pawn.story.WorkTypeIsDisabled( workgiver.workType ) &&
                   ( pawn.story.CombinedDisabledWorkTags & workgiver.workTags ) == WorkTags.None &&
                   ( pawn.story.CombinedDisabledWorkTags & workgiver.workType.workTags ) == WorkTags.None;
        }
    }
}