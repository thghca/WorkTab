﻿// PawnTable_PawnTableOnGUI.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTab
{
    [HarmonyPatch( typeof( PawnTable ), nameof( PawnTable.PawnTableOnGUI ) )]
    public class PawnTable_PawnTableOnGUI
    {
        private static Type       ptt                     = typeof( PawnTable );
        private static MethodInfo RecacheIfDirtyMethod    = AccessTools.Method( ptt, "RecacheIfDirty" );
        private static FieldInfo  cachedColumnWidthsField = AccessTools.Field( ptt, "cachedColumnWidths" );
        private static FieldInfo  cachedRowHeightsField   = AccessTools.Field( ptt, "cachedRowHeights" );
        private static FieldInfo  standardMarginField     = AccessTools.Field( typeof( Window ), "StandardMargin" );

        static PawnTable_PawnTableOnGUI()
        {
            if ( RecacheIfDirtyMethod == null ) throw new NullReferenceException( "RecacheIfDirty field not found." );
            if ( cachedColumnWidthsField == null )
                throw new NullReferenceException( "cachedColumnWidths field not found." );
            if ( cachedRowHeightsField == null )
                throw new NullReferenceException( "cachedRowHeights field not found." );
            if ( standardMarginField == null ) throw new NullReferenceException( "standardMargin field not found." );
        }

        public static bool Prefix( PawnTable __instance, 
                                   Vector2 position, 
                                   PawnTableDef ___def,
                                   ref Vector2 ___scrollPosition )
            //harmony 1.2.0.1 gives access to private fields by ___name.
        {
            if ( ___def != PawnTableDefOf.Work ) // only apply our changes on the work tab.
                return true;

            if ( Event.current.type == EventType.Layout )
                return false;

            RecacheIfDirtyMethod.Invoke( __instance, null );

            // get fields
            var cachedSize              = __instance.Size;
            var columns                 = __instance.ColumnsListForReading;
            var cachedColumnWidths      = cachedColumnWidthsField.GetValue( __instance ) as List<float>;
            var cachedHeaderHeight      = __instance.HeaderHeight;
            var cachedHeightNoScrollbar = __instance.HeightNoScrollbar;
            var headerScrollPosition    = new Vector2( ___scrollPosition.x, 0f );
            var labelScrollPosition     = new Vector2( 0f, ___scrollPosition.y );
            var cachedPawns             = __instance.PawnsListForReading;
            var cachedRowHeights        = cachedRowHeightsField.GetValue( __instance ) as List<float>;
            var standardWindowMargin    = (float) standardMarginField.GetRawConstantValue();

            // this is the main change, vanilla hardcodes both outRect and viewRect to the cached size.
            // Instead, we want to limit outRect to the available view area, so a horizontal scrollbar can appear.
            var labelWidth = cachedColumnWidths[0];
            var labelCol   = columns[0];
            var outWidth   = Mathf.Min( cachedSize.x - labelWidth, UI.screenWidth - standardWindowMargin * 2f );
            var viewWidth  = cachedSize.x - labelWidth - 16f;

            Rect labelHeaderRect = new Rect(
                position.x,
                position.y,
                labelWidth,
                cachedHeaderHeight );

            Rect headerOutRect = new Rect(
                position.x + labelWidth,
                position.y,
                outWidth,
                cachedHeaderHeight );
            Rect headerViewRect = new Rect(
                0f,
                0f,
                viewWidth,
                cachedHeaderHeight );

            Rect labelOutRect = new Rect(
                position.x,
                position.y + cachedHeaderHeight,
                labelWidth,
                cachedSize.y - cachedHeaderHeight );
            Rect labelViewRect = new Rect(
                0f,
                0f,
                labelWidth,
                cachedHeightNoScrollbar - cachedHeaderHeight );

            Rect tableOutRect = new Rect(
                position.x + labelWidth,
                position.y + cachedHeaderHeight,
                outWidth,
                cachedSize.y - cachedHeaderHeight );
            Rect tableViewRect = new Rect(
                0f,
                0f,
                viewWidth,
                cachedHeightNoScrollbar - cachedHeaderHeight );

            // increase height of table to accomodate scrollbar if necessary and possible.
            if ( viewWidth > outWidth && ( cachedSize.y + 16f ) < UI.screenHeight
            ) // NOTE: this is probably optimistic about the available height, but it appears to be what vanilla uses.
                tableOutRect.height += 16f;

            // we need to add a scroll area to the column headers to make sure they stay in sync with the rest of the table, but the first (labels) column should be frozen.
            labelCol.Worker.DoHeader( labelHeaderRect, __instance );

            // scroll area for the rest of the columns - HORIZONTAL ONLY
            var pos = IntVec3.Zero;
            Widgets.BeginScrollView( headerOutRect, ref headerScrollPosition, headerViewRect, false );
            for ( int i = 1; i < columns.Count; i++ )
            {
                int colWidth;
                if ( i == columns.Count - 1 )
                {
                    colWidth = (int) ( viewWidth - pos.x );
                }
                else
                {
                    colWidth = (int) cachedColumnWidths[i];
                }

                Rect rect = new Rect( pos.x, 0f, colWidth, (int) cachedHeaderHeight );
                columns[i].Worker.DoHeader( rect, __instance );
                pos.x += colWidth;
            }
            Widgets.EndScrollView();
            ___scrollPosition.x = headerScrollPosition.x;

            // scrollview for label column - VERTICAL ONLY
            Widgets.BeginScrollView( labelOutRect, ref labelScrollPosition, labelViewRect, false );
            var labelRect = labelOutRect.AtZero();
            for ( int j = 0; j < cachedPawns.Count; j++ )
            {
                // only draw if on screen
                if ( tableViewRect.height                                                  <= tableOutRect.height ||
                     (float) labelRect.y - ___scrollPosition.y + (int) cachedRowHeights[j] >= 0f                 &&
                     (float) labelRect.y                       - ___scrollPosition.y       <= tableOutRect.height )
                {
                    GUI.color = new Color( 1f, 1f, 1f, 0.2f );
                    Widgets.DrawLineHorizontal( 0f, pos.z, tableViewRect.width );
                    GUI.color = Color.white;

                    labelCol.Worker.DoCell( labelRect, cachedPawns[j], __instance );
                    if ( cachedPawns[j].Downed )
                    {
                        GUI.color = new Color( 1f, 0f, 0f, 0.5f );
                        Widgets.DrawLineHorizontal( 0f, labelRect.center.y, labelWidth );
                        GUI.color = Color.white;
                    }
                }

                labelRect.y += (int) cachedRowHeights[j];
            }
            Widgets.EndScrollView();
            ___scrollPosition.y = labelScrollPosition.y;

            // And finally, draw the rest of the table - SCROLLS VERTICALLY AND HORIZONTALLY
            Widgets.BeginScrollView( tableOutRect, ref ___scrollPosition, tableViewRect );
            for ( int j = 0; j < cachedPawns.Count; j++ )
            {
                pos.x = 0;
                // only draw if on screen
                if ( tableViewRect.height                                            <= tableOutRect.height ||
                     (float) pos.y - ___scrollPosition.y + (int) cachedRowHeights[j] >= 0f                 &&
                     (float) pos.y                       - ___scrollPosition.y       <= tableOutRect.height )
                {
                    GUI.color = new Color( 1f, 1f, 1f, 0.2f );
                    Widgets.DrawLineHorizontal( 0f, pos.z, tableViewRect.width );
                    GUI.color = Color.white;
                    Rect rowRect = new Rect( 0f, pos.z, tableViewRect.width, (int) cachedRowHeights[j] );
                    Widgets.DrawHighlightIfMouseover( rowRect );
                    for ( int k = 1; k < columns.Count; k++ )
                    {
                        int cellWidth;
                        if ( k == columns.Count - 1 )
                        {
                            cellWidth = (int) ( viewWidth - pos.x );
                        }
                        else
                        {
                            cellWidth = (int) cachedColumnWidths[k];
                        }

                        Rect rect3 = new Rect( pos.x, pos.y, cellWidth, (int) cachedRowHeights[j] );
                        columns[k].Worker.DoCell( rect3, cachedPawns[j], __instance );
                        pos.x += cellWidth;
                    }

                    if ( cachedPawns[j].Downed )
                    {
                        GUI.color = new Color( 1f, 0f, 0f, 0.5f );
                        Widgets.DrawLineHorizontal( 0f, rowRect.center.y, tableViewRect.width );
                        GUI.color = Color.white;
                    }
                }

                pos.y += (int) cachedRowHeights[j];
            }

            Widgets.EndScrollView();

            return false;
        }
    }
}