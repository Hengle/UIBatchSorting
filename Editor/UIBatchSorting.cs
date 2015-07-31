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
using System;
using System.Text;

public static class UIBatchSorting
{
    public abstract class SortItem : System.IComparable<SortItem> 
    {
        public abstract int Depth { get; set; }
        public string Key { get; protected set; }
        public abstract int CompareTo(SortItem other);
        public abstract bool IsDependent(SortItem other);
    }

    class Node<T>
    {
        public T Data = default(T);
        public List<Node<T>> InData = new List<Node<T>>();
        public List<Node<T>> OutData = new List<Node<T>>();
        public bool IsProcessed;

        public int GetDepth()
        {
            return GetDepth(this);
        }

        int GetDepth(Node<T> node)
        {
            if (node.IsProcessed)
                return 0;
            node.IsProcessed = true;
            
            int depth = 0;
            foreach (var subNode in node.OutData)
            {
                depth = Math.Max(depth, GetDepth(subNode));
            }
            return depth + 1;
        }

        public int GetCount()
        {
            return GetCount(this);
        }

        int GetCount(Node<T> node)
        {
            if (node.IsProcessed)
                return 0;
            node.IsProcessed = true;
            
            int depth = 0;
            foreach (var subNode in node.OutData)
            {
                depth += GetCount(subNode);
            }
            return depth + 1;
        }
    }

    static Node<T> GetNode<T>(Node<T>[] nodes, int index, T data)
    {
        if (nodes[index] == null)
        {
            var node = new Node<T>();
            node.Data = data;
            nodes[index] = node;
        }
        return nodes[index];
    }

    public static SortItem[] Sort(SortItem[] sortItems)
    {
        var sortItemNodes = new Node<SortItem>[sortItems.Length];
        for (var i = 0; i < sortItems.Length; ++i)
        {
            var sortItemNode = GetNode<SortItem>(sortItemNodes, i, sortItems[i]);

            for (var j = 0; j < sortItems.Length; ++j)
            {
                if (i <= j || !sortItems[i].IsDependent(sortItems[j]))
                    continue;

                var checkItemNode = GetNode<SortItem>(sortItemNodes, j, sortItems[j]);
                sortItemNode.InData.Add(checkItemNode);
                sortItems[i].IsDependent(sortItems[j]);
                checkItemNode.OutData.Add(sortItemNode);
            }
        }

        var newData = new List<SortItem>();
        while (true)
        {
            if (Process(sortItemNodes, newData))
                break;
        }

        return newData.ToArray();
    }

    static bool Process(Node<SortItem>[] nodes, List<SortItem> newData)
    {
        List<int> roots, depths, counts;
        AnalyzeNode<SortItem>(nodes, out roots, out depths, out counts);

        var maxRootIndex = FindMaxIndex(depths, counts);
        var maxRootData = nodes[roots[maxRootIndex]].Data;

        newData.Add(maxRootData);
        RemoveNode(nodes, maxRootData);
        
        MoveSameKeyItem(nodes, newData, maxRootData);
        return nodes.Length == newData.Count;
    }

    static void MoveSameKeyItem(Node<SortItem>[] nodes, List<SortItem> newData, SortItem data)
    {
        while (newData.Count < nodes.Length)
        {
            List<int> roots, depths, counts;
            AnalyzeNode<SortItem>(nodes, out roots, out depths, out counts);

            var start = newData.Count;
            foreach (var root in roots)
            {
                if (nodes[root].Data.Key == data.Key)
                {
                    newData.Add(nodes[root].Data);
                }
            }

            if (start == newData.Count)
                break;

            for (var i = start; i < newData.Count; ++i)
                RemoveNode(nodes, newData[i]);
        }
    }

    static void RemoveNode(Node<SortItem>[] nodes, SortItem data)
    {
        for (var i = 0; i < nodes.Length; ++i)
        {
            var node = nodes[i];
            if (nodes[i] == null)
                continue;
            
            if (object.ReferenceEquals(node.Data, data))
            {
                nodes[i] = null;
                continue;
            }
            
            for (var j = node.InData.Count; j > 0; j--)
            {
                var subNode = node.InData[j - 1];
                if (object.ReferenceEquals(subNode.Data, data))
                    node.InData.RemoveAt(j - 1);
            }
            
            for (var j = node.OutData.Count; j > 0; j--)
            {
                var subNode = node.OutData[j - 1];
                if (object.ReferenceEquals(subNode.Data, data))
                    node.OutData.RemoveAt(j - 1);
            }
        }
    }

    static void ResetNodes<T>(Node<T>[] nodes)
    {
        Array.ForEach(nodes, (tempNode) => {
            if (tempNode != null)
                tempNode.IsProcessed = false;
        });
    }

    static void AnalyzeNode<T>(Node<T>[] nodes, out List<int> roots, out List<int> depths, out List<int> counts)
    {
        roots = new List<int>();
        depths = new List<int>();
        counts = new List<int>();
        
        for (var i = 0; i < nodes.Length; ++i)
        {
            if (nodes[i] == null)
                continue;
            
            var node = nodes[i];
            if (node.InData.Count != 0)
                continue;

            roots.Add(i);

            ResetNodes<T>(nodes);
            depths.Add(node.GetDepth());

            ResetNodes<T>(nodes);
            counts.Add(node.GetCount());
        }
        
        if (roots.Count == 0)
            throw new NotSupportedException("can not sort!");
    }

    static int FindMaxIndex(List<int> first, List<int> second)
    {
        var index = -1;
        for (var i = 0; i < first.Count; ++i)
        {
            if (index == -1 || first[i] > first[index])
                index = i;
            else if (first[i] == first[index] && second[i] > second[index])
                index = i;
        }
        return index;
    }

    public static void AdjustDepth(SortItem[] items)
    {
        var start = 0;
        for (var i = 0; i < items.Length; ++i)
        {
            if (items[i].Key == items[start].Key)
                continue;
            
            AdjustRangeDepth(items, start, i);
            start = i;
        }
    }
    
    static void AdjustRangeDepth(SortItem[] items, int start, int end)
    {
        if (start >= end)
            throw new System.ArgumentException("start must be less than end.");
        
        int minDepth = int.MaxValue;
        int lowerDepth = int.MinValue;
        if (start > 0)
            lowerDepth = items[start - 1].Depth + 1;

        for (int i = start; i < end; ++i)
        {
            minDepth = System.Math.Min(minDepth, System.Math.Max(items[i].Depth, lowerDepth));
        }

        int depth = minDepth;
        for (int i = start; i < end; ++i)
        {
            if (items[i].Depth == minDepth)
            {
                items[i].Depth = depth;
            }
            else if (items[i].Depth > minDepth)
            {
                minDepth = items[i].Depth;
                items[i].Depth = ++depth;
            }
        }
        
        for (int i = end; i < items.Length; ++i)
        {
            if (depth >= items[i].Depth)
                items[i].Depth = depth + 1;
        }
    }

    public static int GetDrawCallCount(SortItem[] items)
    {
        if (items.Length == 0)
            return 0;

        var count = 1;
        var start = 0;
        for (var i = 0; i < items.Length; ++i)
        {
            if (items[i].Key == items[start].Key)
                continue;

            count++;
            start = i;
        }

        return count;
    }
}
