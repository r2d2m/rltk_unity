﻿using NUnit.Framework;
using RLTK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[TestFixture]
public class FOVTests
{
    public struct TestMap : FOV.IVisibilityMap, IDisposable
    {
        int width;
        int height;
        public NativeArray<bool> opaquePoints;

        public TestMap(int width, int height, Allocator allocator, params int2[] opaquePoints)
        {
            this.width = width;
            this.height = height;
            this.opaquePoints = new NativeArray<bool>(width * height, allocator);
            foreach (var p in opaquePoints)
                this.opaquePoints[p.y * width + p.x] = true;
        }
        
        public bool IsInBounds(int2 p) => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;

        public bool IsOpaque(int2 p) => opaquePoints[p.y * width + p.x];

        public void Dispose() => opaquePoints.Dispose();
    }

    [Test]
    public void TestFOV()
    {
        var map = new TestMap(20, 20, Allocator.TempJob,
            new int2(1, 1),
            new int2(2, 1));

        int range = 5;

        var points = new NativeList<int2>((range * 2) * (range * 2), Allocator.TempJob);
        FOV.GetVisiblePointsJob(0, 5, map, points).Run();

        Assert.False(points.Contains(new int2(3, 3)));
        Assert.True(points.Contains(new int2(2, 1)));
        Assert.True(points.Contains(new int2(1, 1)));

        points.Dispose();
        map.Dispose();
    }

    [BurstCompile]
    struct FOVJob : IJob
    {
        public int2 origin;
        public int range;
        public TestMap map;
        public NativeList<int2> buffer;

        public void Execute()
        {
            FOV.GetVisiblePoints(origin, range, map, buffer);
        }
    }

    [Test]
    public void UseFOVInsideJob()
    {
        var map = new TestMap(20, 20, Allocator.TempJob,
        new int2(1, 1),
        new int2(2, 1));

        int range = 5;

        var points = new NativeList<int2>((range * 2) * (range * 2), Allocator.TempJob);

        new FOVJob
        {
            origin = 0,
            range = 5,
            map = map,
            buffer = points
        }.Schedule().Complete();
        
        Assert.False(points.Contains(new int2(3, 3)));
        Assert.True(points.Contains(new int2(2, 1)));
        Assert.True(points.Contains(new int2(1, 1)));

        points.Dispose();
        map.Dispose();
    }

    [Test]
    public void ViewshedContainsNoDuplicates()
    {
        var map = new TestMap(20, 20, Allocator.TempJob,
        new int2(1, 1),
        new int2(2, 1));

        int range = 5;

        var points = new NativeList<int2>((range * 2) * (range * 2), Allocator.TempJob);
        FOV.GetVisiblePointsJob(0, 5, map, points).Run();

        var arr = points.ToArray();
        Assert.AreEqual(arr.Length, arr.Distinct().ToArray().Length);

        points.Dispose();
        map.Dispose();
    }



}
