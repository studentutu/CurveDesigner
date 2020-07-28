﻿using Assets.NewUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;


[CustomEditor(typeof(Curve3D))]
public class Curve3DInspector : Editor
{
    private static readonly int _CurveHint = "NewGUI.CURVE".GetHashCode();

    MonoScript script;
    private void OnEnable()
    {
        script = MonoScript.FromMonoBehaviour((Curve3D)target);
    }

    public override void OnInspectorGUI()
    {
        var curve3d = (target as Curve3D);

        GUI.enabled = false;
        script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
        GUI.enabled = true;

        float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
        EditorGUIUtility.labelWidth = 0;
        EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth - 4;

        EditorGUILayout.BeginVertical();
        GUIStyle effectBgStyle = "ShurikenEffectBg";
        GUIStyle shurikenModuleBg = "ShurikenModuleBg";
        GUIStyle mixedToggleStyle = "ShurikenToggleMixed";
        GUIStyle initialHeaderStyle = "ShurikenEmitterTitle";
        GUIStyle nonInitialHeaderStyle = "ShurikenModuleTitle";
        bool isDisabled = false;

        GUILayout.BeginVertical(effectBgStyle);
        {
            for (int i = 0; i < curve3d.collapsableCategories.Length; i++)
            {
                var curr = curve3d.collapsableCategories[i];
                bool isInitial = i == 0;
                GUIStyle headerStyle;
                int headerHeight;
                if (isInitial)
                {
                    headerHeight = 25;
                    headerStyle = initialHeaderStyle;
                } else
                {
                    headerHeight = 15;
                    headerStyle = nonInitialHeaderStyle;
                }
                Rect headerRect = GUILayoutUtility.GetRect(width, headerHeight);
                int iconSize = 21;
                Rect iconRect = new Rect(headerRect.x + 4, headerRect.y + 2, iconSize, iconSize);
                if (curr.isExpanded)
                {
                    using (new EditorGUI.DisabledScope(isDisabled))
                    {
                        GUIStyle m_ModulePadding = new GUIStyle();
                        m_ModulePadding.padding = new RectOffset(3, 3, 4, 2);
                        Rect moduleSize = EditorGUILayout.BeginVertical(m_ModulePadding);
                        {
                            moduleSize.y -= 4;
                            moduleSize.height += 4;
                            GUI.Label(moduleSize, GUIContent.none, shurikenModuleBg);
                            curr.Draw(curve3d);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                if (isInitial && curve3d.settings.uiIcon!=null)
                {
                    GUI.DrawTexture(iconRect, curve3d.settings.uiIcon, ScaleMode.StretchToFill, true);
                }
                GUIContent headerLabel = new GUIContent();
                headerLabel.text = curr.GetName(curve3d);
                curr.isExpanded = GUI.Toggle(headerRect, curr.isExpanded, headerLabel, headerStyle);
                GUILayout.Space(1);
            }
            GUILayout.Space(-1);
        }
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();

        Undo.RecordObject(curve3d, "curve");

        int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.KeyDown:
                HandleKeys(curve3d);
                break;
        }


        //GUILayout.Label("asdf"); 
        //base.OnInspectorGUI();
    }

    private void WindowFunc(int id)
    {
        var curve = (target as Curve3D);
        var editModes = curve.editModeCategories;
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        int skipCount = 0;
        if (curve.type != CurveType.DoubleBezier)
            skipCount++;
        for (int i = 0; i < editModes.editModes.Length; i++)
        {
            EditMode currMode = editModes.editModes[i];
            if (curve.type != CurveType.DoubleBezier && currMode == EditMode.DoubleBezier)
                continue;
            string currName = editModes.editmodeNameMap[currMode];
            string style;
            if (i == 0)
                style = "ButtonLeft";
            else if (i-skipCount == editModes.editModes.Length - 1 - skipCount)
                style = "ButtonRight";
            else
                style = "ButtonMid";
            if (GUILayout.Toggle(curve.editMode == currMode, EditorGUIUtility.TrTextContent(currName), style))
                curve.editMode = currMode;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Clear"))
        {
            curve.positionCurve = new BezierCurve();
            curve.positionCurve.owner = curve;
            curve.positionCurve.Initialize();
            curve.positionCurve.isCurveOutOfDate = true;
            curve.sizeSampler = new  FloatDistanceSampler("Size");
            curve.rotationSampler = new FloatDistanceSampler("Rotation (degrees)");
            curve.doubleBezierSampler = new DoubleBezierSampler();
            curve.UICurve = new UICurve(null, curve);
            curve.UICurve.Initialize();
            Debug.Log("cleared");
        }
        GUILayout.EndVertical();
        if (curve.UICurve != null)
        {
            var drawer = curve.UICurve.GetWindowDrawer();
            drawer.DrawWindow(curve);
        }
        if (Event.current.type == EventType.MouseUp)
        {
            //Debug.Log("asdfasf");
            //Event.current.Use();
            //int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
                        //GUIUtility.hotControl = controlID;
        }
    }

    void HandleKeys(Curve3D curve3d)
    {
        if (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace)
        {
            bool didDelete = curve3d.ActiveElement.Delete(curve3d.selectedPoints, curve3d);
            if (didDelete)
            {
                curve3d.RequestMeshUpdate();
                curve3d.UICurve.Initialize();
            }
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.A && Event.current.control)
        {
            curve3d.selectedPoints = curve3d.ActiveElement.SelectAll(curve3d);
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.Tab)
        {
            switch (curve3d.editMode)
            {
                case EditMode.PositionCurve:
                    curve3d.editMode = EditMode.Rotation;
                    break;
                case EditMode.Rotation:
                    curve3d.editMode = EditMode.Size;
                    break;
                case EditMode.Size:
                    curve3d.editMode = EditMode.PositionCurve;
                    break;
            }
            Event.current.Use();
        }
        SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        var curve3d = (target as Curve3D);
        var windowRect = new Rect(20, 20, 0, 0);
        MouseEater.EatMouseInput(GUILayout.Window(61732234, windowRect, WindowFunc, $"Editing {curve3d.editModeCategories.editmodeNameMap[curve3d.editMode]}"));

        curve3d.positionCurve.owner = curve3d;
        curve3d.positionCurve.isClosedLoop = curve3d.isClosedLoop;
        curve3d.positionCurve.dimensionLockMode = curve3d.lockToPositionZero;
        curve3d.positionCurve.Recalculate();
        var secondaryCurves = curve3d.doubleBezierSampler.points;
        if (secondaryCurves.Count > 0)
        {
            foreach (var curr in secondaryCurves)
                curr.value.owner = curve3d;//gotta be careful that I'm not referencing stuff in owner that I shouldn't be
            var referenceHint = secondaryCurves[0].value.Recalculate();
            for (int i = 1; i < secondaryCurves.Count; i++)
                referenceHint = secondaryCurves[i].value.Recalculate(referenceHint);
        }
        curve3d.CacheAverageSize();
        var rotationPoints = curve3d.rotationSampler.GetPoints(curve3d.positionCurve);
        if (curve3d.previousRotations.Count != rotationPoints.Count)
            curve3d.CopyRotations();
        Undo.RecordObject(curve3d, "curve");
        ClickHitData elementClickedDown = curve3d.elementClickedDown;
        Curve3DSettings.circleTexture = curve3d.settings.circleIcon;
        Curve3DSettings.squareTexture = curve3d.settings.squareIcon;
        Curve3DSettings.diamondTexture = curve3d.settings.diamondIcon;
        Curve3DSettings.defaultLineTexture = curve3d.settings.lineTex;
        if (curve3d.UICurve == null)
        {
            curve3d.UICurve=new UICurve(null,curve3d);
            curve3d.UICurve.Initialize();
        }
        UpdateMesh(curve3d);
        var curveEditor = curve3d.UICurve;
        var MousePos = Event.current.mousePosition;
        bool IsActiveElementSelected()
        {
            return curve3d.selectedPoints.Where(a => a == curve3d.elementClickedDown.owner.GUID).Count() > 0;
        }
        int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
        var eventType = Event.current.GetTypeForControl(controlID);
        switch (eventType)
        {
            case EventType.KeyDown:
                HandleKeys(curve3d);
                break;
            case EventType.Repaint:
                Draw(curveEditor, MousePos, elementClickedDown,curve3d.selectedPoints);
                break;
            case EventType.MouseDown:
                if (Event.current.button == 0)
                {
                    var clicked = GetClosestElementToCursor(curveEditor, MousePos, EventType.MouseDown);
                    if (clicked != null)
                    {
                        GUIUtility.hotControl = controlID;
                        curve3d.elementClickedDown = clicked;
                        var clickPos = MousePos + curve3d.elementClickedDown.offset;
                        var commandToExecute = curve3d.elementClickedDown.owner.GetClickCommand();
                        //shift will behave like control if it's a split command, also just fixed a bug when shift-clicking a split point
                        bool isSplitAndShift = Event.current.shift && (commandToExecute is SplitCommand);
                        if (Event.current.control || isSplitAndShift)  /////CONTROL CLICK
                        {
                            curve3d.shiftControlState = Curve3D.ClickShiftControlState.control;
                            curve3d.ToggleSelectPoint(curve3d.elementClickedDown.owner.Guid);
                        }
                        else if (Event.current.shift) /////SHIFT CLICK
                        {
                            curve3d.shiftControlState = Curve3D.ClickShiftControlState.shift;
                            SelectableGUID previous = SelectableGUID.Null;
                            if (curve3d.selectedPoints.Count > 0)
                                previous = curve3d.selectedPoints[0];
                            curve3d.selectedPoints = SelectableGUID.SelectBetween(curve3d.ActiveElement, previous, curve3d.elementClickedDown.owner.Guid,curve3d,curve3d.positionCurve);//for a double bezier selection should use that curve instead of main
                        }
                        else
                        {
                            curve3d.shiftControlState = Curve3D.ClickShiftControlState.none;
                            if (curve3d.selectedPoints.Contains(curve3d.elementClickedDown.owner.Guid))
                                curve3d.SelectAdditionalPoint(curve3d.elementClickedDown.owner.Guid);
                            else 
                                curve3d.SelectOnlyPoint(curve3d.elementClickedDown.owner.Guid);
                        }
                        if (IsActiveElementSelected())
                        {
                            Draw(curveEditor, MousePos, clicked ,curve3d.selectedPoints,true); //this fixes it
                            //commandToExecute.ClickDown(clickPos,curve3d,curve3d.selectedPoints);
                            //commandToExecute.ClickDrag(clickPos, curve3d, curve3d.elementClickedDown,curve3d.selectedPoints);
                        }
                        curve3d.RequestMeshUpdate();
                        Event.current.Use();
                    }
                }
                break;
            case EventType.MouseDrag:
                if (elementClickedDown != null)
                {
                    var clickPos = MousePos + elementClickedDown.offset;
                    elementClickedDown.hasBeenDragged = true;
                    var commandToExecute = elementClickedDown.owner.GetClickCommand();
                    if (IsActiveElementSelected())
                        commandToExecute.ClickDrag(clickPos,curve3d,elementClickedDown,curve3d.selectedPoints);
                    Event.current.Use();
                    curve3d.RequestMeshUpdate();
                }
                break;
            case EventType.MouseUp:
                if (Event.current.button == 0)
                {
                    if (elementClickedDown != null)
                    {
                        GUIUtility.hotControl = 0;
                        var commandToExecute = elementClickedDown.owner.GetClickCommand();
                        if (IsActiveElementSelected())
                            commandToExecute.ClickUp(MousePos,curve3d,curve3d.selectedPoints);
                        curve3d.RequestMeshUpdate();
                        if (!curve3d.elementClickedDown.hasBeenDragged && curve3d.shiftControlState==Curve3D.ClickShiftControlState.none)
                        {
                            curve3d.SelectOnlyPoint(curve3d.elementClickedDown.owner.Guid);
                        }
                        curve3d.elementClickedDown = null; 
                        Event.current.Use();
                    }
                }
                break;
            case EventType.MouseMove:
                Draw(curveEditor, MousePos, elementClickedDown,curve3d.selectedPoints,true);
                HandleUtility.Repaint();
                break;
            default:
                Draw(curveEditor, MousePos, elementClickedDown,curve3d.selectedPoints,true);
                break;
        }
        curve3d.CopyRotations();
    }
    private void UpdateMesh(Curve3D curve)
    {
        if (!MeshGenerator.IsBuzy)
        {
            if (curve.lastMeshUpdateEndTime != MeshGenerator.lastUpdateTime)
            {
                if (MeshGenerator.didMeshGenerationSucceed)
                {
                    if (curve.displayMesh == null)
                    {
                        curve.displayMesh = new Mesh();
                        curve.filter.mesh = curve.displayMesh;
                    }
                    else
                    {
                        curve.displayMesh.Clear();
                    }
                    curve.displayMesh.SetVertices(MeshGenerator.vertices);
                    curve.displayMesh.SetTriangles(MeshGenerator.triangles, 0);
                    curve.displayMesh.SetUVs(0, MeshGenerator.uvs);
                    curve.displayMesh.RecalculateNormals();
                }
                curve.lastMeshUpdateEndTime = MeshGenerator.lastUpdateTime;
            }
            if (curve.lastMeshUpdateStartTime != MeshGenerator.lastUpdateTime)
            {
                MeshGenerator.StartGenerating(curve);
            }
        }
        if (curve.HaveCurveSettingsChanged())
        {
            curve.RequestMeshUpdate();
        }
    }
    private const float BoundsExtension = 20;
    ClickHitData GetClosestElementToCursor(IComposite root,Vector2 clickPosition,EventType eventType)
    {
        ClickHitData GetFrom(IEnumerable<ClickHitData> lst)
        {
            var primaries = lst.Where(a => a.owner.IsWithinBounds(clickPosition)).OrderBy(a => a.distanceFromCamera);
            if (primaries.Count() > 0)
                return primaries.First();
            //we didn't hit any primary hits, so we are gonna just pick the thing with min distanceFromBounds
            var secondaries = lst.Where(a => a.owner.distanceFromBounds < BoundsExtension).OrderBy(a => a.owner.distanceFromBounds);
            if (secondaries.Count() > 0)
                return secondaries.First();
            return null;
        }
        List<ClickHitData> hits = new List<ClickHitData>();
        root.Click(clickPosition, hits, eventType);
        var highPriority = hits.Where(a => !a.isLowPriority);
        var high = GetFrom(highPriority);
        if (high != null)
            return high;
        var lowPriority = hits.Where(a => a.isLowPriority);
        var low = GetFrom(lowPriority);
        return low;
    }

    void Draw(IComposite root,Vector2 mousePos,ClickHitData currentlyHeldDown,List<SelectableGUID> selected,bool imgui=false)
    {
        ClickHitData closestElementToCursor = null;
        if (currentlyHeldDown==null)
            closestElementToCursor = GetClosestElementToCursor(root,mousePos, EventType.Repaint);
        List<IDraw> draws = new List<IDraw>();
        root.Draw(draws,closestElementToCursor);
        draws.Sort((a, b) => (int)(Mathf.Sign(b.DistFromCamera() - a.DistFromCamera())));
        foreach (var draw in draws)
        {
            SelectionState selectionState = SelectionState.unselected;
            var guid = draw.Creator().GUID;
            if (selected.Count>0 && guid==selected[0])
                selectionState = SelectionState.primarySelected;
            else if (selected.Contains(guid))
                selectionState = SelectionState.secondarySelected;
            if (draw.DistFromCamera() > 0)
            {
                if (imgui)
                {
                    draw.Event();
                }
                else
                {
                    if (closestElementToCursor != null && draw.Creator() == closestElementToCursor.owner)
                        draw.Draw(DrawMode.hovered, selectionState);
                    else if (currentlyHeldDown != null && draw.Creator() == currentlyHeldDown.owner)
                        draw.Draw(DrawMode.clicked, selectionState);
                    else
                        draw.Draw(DrawMode.normal, selectionState);
                }
            }
        }
    }
}
/*
//we could dick around trying to draw our own lines/icons so that we can sort using the depth buffer, but I say we do that later
GL.PushMatrix();
curve3d.testmat.SetPass(0);
GL.LoadOrtho();
GL.Begin(GL.TRIANGLES);
GL.Color(Color.green);
GL.Vertex3(0, 0, -10f);
GL.Vertex3(1, 1, -10f);
GL.Vertex3(0, 1, -10f);
GL.Color(Color.blue);
GL.Vertex3(0, 0, -5f);
GL.Vertex3(1, 1, -5f);
GL.Vertex3(0, 1, -5f);
GL.End();
GL.PopMatrix();
*/
