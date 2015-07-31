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

[ExecuteInEditMode]
public static partial class UIBatchSortingTest
{
    class TestSortItem : UIBatchSorting.SortItem
    {
        public TestSortItem(int depth, string key, int[] group)
        {
            Depth = depth;
            Key = key;
            Group = group;
        }

        public override int CompareTo(UIBatchSorting.SortItem other)
        {
            return this.Depth.CompareTo(((TestSortItem)other).Depth);
        }

        public override bool IsDependent(UIBatchSorting.SortItem other)
        {
            return System.Array.FindIndex(Group, depth => {return depth == ((TestSortItem)other).Depth;}) != -1;
        }

        public override string ToString()
        {
            return string.Format("D:{0}", Depth);
        }

        public override int Depth { get; set; }
        int[] Group;
    }

    [MenuItem("XStudio/Test/Batch Sorting Test %F12")]
    public static void BatchSortingTestMenu()
    {
        TestOverlop();
        TestParseGameObject();
        TestSortWidgets();
    }

    [MenuItem("XStudio/Test/Active All GameObject")]
    public static void ActiveAllGameObject()
    {
        var go = Selection.activeGameObject;
        go.transform.Traversal(tr=>{
            tr.gameObject.SetActive(true);
            return true;
        });
    }
    
    static void TestOverlop()
    {
        var rect = new Rect(0, 0, 100, 100);
        Assert(true, rect.Overlaps(new Rect(-1, -1, 101, 101)));
        Assert(true, rect.Overlaps(new Rect(1, 1, 50, 50)));
        Assert(true, rect.Overlaps(new Rect(50, 50, 200, 200)));
        Assert(false, rect.Overlaps(new Rect(150, 150, 200, 200)));
    }

    static void TestSortWidgets()
    {
        var sortItems = new List<UIBatchSorting.SortItem>();
        var keys = new string[]{"1", "2", "2", "3", "3", "4", "2", "3", "2", "1"}; 
        var groups = new int[][]{
            new int[]{1 ,2},
            new int[]{0, 2, 3}, 
            new int[]{0, 1, 3}, 
            new int[]{1, 2, 5}, 
            new int[]{2, 5}, 
            new int[]{3, 4},
            new int[]{7, 8},
            new int[]{6, 9},
            new int[]{6, 9},
            new int[]{7, 8},
        };

        for (int i = 0; i < groups.Length; ++i)
        {
            sortItems.Add(new TestSortItem(i, keys[i], groups[i]));
        }

        sortItems.Sort();
        var newSortItems = UIBatchSorting.Sort(sortItems.ToArray());
        Debug.Log(string.Format("DrawCall\t{0}\t{1}", 
                                UIBatchSorting.GetDrawCallCount(sortItems.ToArray()), 
                                UIBatchSorting.GetDrawCallCount(newSortItems)));
        for (var i = 0; i < sortItems.Count; ++i)
        {
            Debug.Log(string.Format("{0}\t\t{1}", sortItems[i].ToString(), newSortItems[i].ToString()));
        }

        UIBatchSorting.AdjustDepth(newSortItems);
        for (var i = 0; i < newSortItems.Length; ++i)
        {
            Debug.Log(string.Format("{0}", newSortItems[i].ToString()));
        }
    }

    static void TestParseGameObject()
    {
        var go = new GameObject("Test");
        go.AddComponent<UIRoot>();

        try
        {
            var goPanel = new GameObject();
            var rootPanel = goPanel.AddComponent<UIPanel>();
            goPanel.transform.parent = go.transform;
            
            for (int i = 0; i < 5; ++i)
            {
                var goWidget = new GameObject();
                goWidget.AddComponent<UIWidget>();
                goWidget.transform.parent = goPanel.transform;
                
                for (int j = 0; j < 10; ++j)
                {
                    var goWidgetSub = new GameObject();
                    if (i % 2 == 1)
                        goWidgetSub.AddComponent<UIWidget>();
                    goWidgetSub.transform.parent = goWidget.transform;
                }
            }
            
            {
                var panels = NGUIBatchSorting.GetUIPanels(go);
                Assert(panels.Length, 1);
                Assert(panels[0], rootPanel);
                
                var widgets = NGUIBatchSorting.GetUIWidgets(rootPanel);
                Assert(widgets.Length, 25);
            }
            
            for (int i = 0; i < 2; ++i)
            {
                var goPanelSub = new GameObject();
                goPanelSub.AddComponent<UIPanel>();
                goPanelSub.transform.parent = goPanel.transform;
                
                for (int j = 0; j < 5; ++j)
                {
                    var goWidget = new GameObject();
                    goWidget.AddComponent<UIWidget>();
                    goWidget.transform.parent = goPanelSub.transform;
                }
            }
            
            {
                var panels = NGUIBatchSorting.GetUIPanels(go);
                Assert(panels.Length, 3);
                
                foreach (var panel in panels)
                {
                    var widgets = NGUIBatchSorting.GetUIWidgets(panel);
                    
                    if (panel == rootPanel)
                        Assert(widgets.Length, 25);
                    else
                        Assert(widgets.Length, 5);
                }
            }
        }
        finally
        {
            GameObject.DestroyImmediate(go);
        }
    }

    static void Assert(object expect, object actual)
    {
        if (object.Equals(expect, actual))
            return;
        
        Debug.LogError(string.Format("Except:{0} Actual:{1}", expect, actual));
    }
}
