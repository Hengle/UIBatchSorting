/****************************************************************************
Copyright (c) 2014 dpull.com

http://www.dpull.com

****************************************************************************/

using UnityEditorInternal;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

[ExecuteInEditMode]
public class NGUIBatchSorting
{
    class UIWidgetSortItem : UIBatchSorting.SortItem
    {      
        public UIWidgetSortItem(UIPanel panel, UIWidget widget)
        {
            Panel = panel;
            Widget = widget;

            Key = string.Format("Material:{0};Texture:{1};Shader:{2}", 
                                        Widget.material == null ? string.Empty : Widget.material.GetInstanceID().ToString(), 
                                        Widget.mainTexture == null ? string.Empty : Widget.mainTexture.GetInstanceID().ToString(), 
                                        Widget.shader == null ? string.Empty : Widget.shader.GetInstanceID().ToString());

            var corners = Widget.worldCorners;
            for (int i = 0; i < corners.Length; i += 2)
                corners[i] = Panel.transform.InverseTransformPoint(corners[i]);
            Border = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        public override int Depth
        { 
            get
            {
                return Widget.depth;
            }
            set
            {
                Widget.depth = value;
            }
        }

        public override int CompareTo(UIBatchSorting.SortItem other)
        {
            return UIWidget.PanelCompareFunc(this.Widget, ((UIWidgetSortItem)other).Widget);
        }

        public override bool IsDependent(UIBatchSorting.SortItem other)
        {
            return Border.Overlaps(((UIWidgetSortItem)other).Border);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var transform = Widget.transform;
            while (transform != null && transform != Panel.transform)
            {
                sb.Insert(0, string.Format("/{0}", transform.name));
                transform = transform.parent;
            }
            sb.Insert(0, Panel.name);
            return sb.ToString();
        }

        UIPanel Panel;
        UIWidget Widget;
        Rect Border;
    }
    
    [MenuItem("XStudio/UI/Batch Sorting")]
    public static void BatchSortingMenu()
    {
        var go = Selection.activeGameObject;
        ProcessNGUIBatchSorting(go);
    }

    static void ProcessNGUIBatchSorting(GameObject go)
    {
        var sortInfo = new StringBuilder();

        var panels = GetUIPanels(go);
        foreach (var panel in panels)
        {
            var widgets = GetUIWidgets(panel);
            var sortItems = BuildUIWidgetSortItem(panel, widgets);
            if (sortItems.Length == 0)
            {
                Debug.LogWarning("UIPanel不存在渲染对象" + panel.gameObject.GetFullName(go.transform));
                continue;
            }

//            Debug.Log(UIBatchSorting.GetDrawCallCount(sortItems));
//            
//            var start = 0;
//            for (var i = 0; i < sortItems.Length; ++i)
//            {
//                if (sortItems[i].Key == sortItems[start].Key)
//                    continue;
//                Debug.Log(string.Format("{0}\t{1}", i - start, sortItems[start].ToString()));
//                start = i;
//            }
//            continue;
            
            var newSortItems = UIBatchSorting.Sort(sortItems);
            var sortItemsDrawCallCount = UIBatchSorting.GetDrawCallCount(sortItems);
            var newSortItemsCount = UIBatchSorting.GetDrawCallCount(newSortItems);

            if (sortItemsDrawCallCount == newSortItemsCount)
                continue;

            UIBatchSorting.AdjustDepth(newSortItems);
            sortInfo.AppendFormat("{0}优化了:{1}DrawCall.({2}=>{3})\n",
                               panel.name, 
                               sortItemsDrawCallCount - newSortItemsCount, 
                               sortItemsDrawCallCount, 
                               newSortItemsCount);
        }

        Debug.Log(sortInfo.ToString());
    }

    delegate bool TraversalCallback(GameObject go);
    static bool Traversal(GameObject go, TraversalCallback callback)
    {
        if (!callback(go))
            return false;
        
        foreach (Transform child in go.transform)
        {
            Traversal(child.gameObject, callback);
        }
        return true;
    }
    
    public static UIPanel[] GetUIPanels(GameObject go)
    {
        var panels = new List<UIPanel>();
        
        Traversal(go, child => {
            var panel = child.GetComponent<UIPanel>();
            if (panel != null)
                panels.Add(panel);
            return true;
        });
        
        return panels.ToArray();
    }
    
    public static UIWidget[] GetUIWidgets(UIPanel panel)
    {
        var widgets = new List<UIWidget>();
        
        Traversal(panel.gameObject, child => {
            var childPanel = child.GetComponent<UIPanel>();
            if (childPanel != null && childPanel != panel)
                return false;
            
            var widget = child.GetComponent<UIWidget>();
            if (widget != null)
                widgets.Add(widget);
            
            return true;
        });
        
        return widgets.ToArray();
    }
    
    static UIBatchSorting.SortItem[] BuildUIWidgetSortItem(UIPanel panel, UIWidget[] widgets)
    {
        var sortItems = new List<UIBatchSorting.SortItem>();
        foreach (var widget in widgets)
        {
            if (widget.material == null && widget.mainTexture == null && widget.shader == null)
                continue;
            
            var sortItem = new UIWidgetSortItem(panel, widget);
            sortItems.Add(sortItem);
        }
        
        sortItems.Sort();
        return sortItems.ToArray();
    }
}

