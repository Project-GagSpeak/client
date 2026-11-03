using CkCommons;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using GagSpeak.Services.Mediator;
using Layer = FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;

namespace GagSpeak.PlayerControl;

/// <summary>
///   Navmeshes of the current housing interior, built from its collision meshes so they include railings and
///   furniture. Paths use the roomy mesh (clear of walls), and the tight one only for gaps the roomy one closes.
/// </summary>
public sealed unsafe class HousingNavmesh : DisposableMediatorSubscriberBase
{
    private enum Shape { Mesh, Box, Cylinder, Plane }
    private readonly record struct Part(Shape Shape, string Path, Matrix4x4 World);
    private readonly record struct Mesh(Vector3[] Verts, int[] Tris);
    private readonly record struct MeshPath(List<Vector3> Points, bool StartOnMesh, bool EndOnMesh, bool Partial);

    // Unit shapes matching the game's analytic colliders, scaled into place by each part's transform.
    private static readonly Mesh Box = new(
        [new(-1, -1, -1), new(-1, -1, 1), new(1, -1, 1), new(1, -1, -1), new(-1, 1, -1), new(-1, 1, 1), new(1, 1, 1), new(1, 1, -1)],
        [4, 5, 6, 4, 6, 7, 0, 2, 1, 0, 3, 2, 0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2, 0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5]);
    private static readonly Mesh Plane = new(
        [new(-1, 1, 0), new(-1, -1, 0), new(1, -1, 0), new(1, 1, 0)],
        [0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2]); // both sides
    private static readonly Mesh Cylinder = BuildCylinder(12);
    private static readonly RcVec3f SearchExtents = new(2, 4, 2);
    private const int MaxPath = 256;

    private readonly IDtQueryFilter _filter = new DtQueryDefaultFilter();
    private CancellationTokenSource? _buildCts;
    private DtNavMeshQuery? _roomy;
    private DtNavMeshQuery? _tight;

    public HousingNavmesh(ILogger<HousingNavmesh> logger, GagspeakMediator mediator) : base(logger, mediator)
    {
        Mediator.Subscribe<TerritoryChanged>(this, _ => Clear());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => TryStartBuild());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Clear();
    }

    public bool Ready => _roomy is not null && _tight is not null;

    /// <summary> Waypoints from <paramref name="from"/> to <paramref name="to"/>, or null if there is no mesh or no path. </summary>
    public List<Vector3>? FindPath(Vector3 from, Vector3 to)
    {
        if (_roomy is not { } roomy || _tight is not { } tight)
            return null;

        // Use the tight mesh where the roomy one falls short: starting off its edge, or from where it ends to the goal.
        if (Query(roomy, from, to) is not { StartOnMesh: true } roomyPath)
            return Finish(Query(tight, from, to), to);
        if (!roomyPath.Partial && roomyPath.EndOnMesh)
            return Finish(roomyPath, to);
        if (Query(tight, roomyPath.Points[^1], to) is not { } rest)
            return roomyPath.Points;

        roomyPath.Points.AddRange(rest.Points.Skip(1));
        return Finish(rest with { Points = roomyPath.Points }, to);
    }

    private List<Vector3>? Finish(MeshPath? path, Vector3 to)
    {
        if (path is not { } p)
            return null;

        // A partial path stops at the closest reachable point, so don't step on into whatever blocks the goal.
        if (p.Partial)
        {
            Logger.LogDebug($"Navmesh path to {to:F2} is partial, it ends {Vector3.Distance(p.Points[^1], to):F2}y short.");
            return p.Points;
        }

        // The mesh stops short of obstacles, so finish with a short step onto the goal.
        if (Vector2.Distance(new(p.Points[^1].X, p.Points[^1].Z), new(to.X, to.Z)) > 0.1f)
            p.Points.Add(to);
        return p.Points;
    }

    private MeshPath? Query(DtNavMeshQuery query, Vector3 from, Vector3 to)
    {
        query.FindNearestPoly(new RcVec3f(from.X, from.Y, from.Z), SearchExtents, _filter, out var startRef, out var startPt, out var startOver);
        query.FindNearestPoly(new RcVec3f(to.X, to.Y, to.Z), SearchExtents, _filter, out var endRef, out var endPt, out var endOver);
        if (startRef == 0 || endRef == 0)
            return null;

        Span<long> polys = stackalloc long[MaxPath];
        var status = query.FindPath(startRef, endRef, startPt, endPt, _filter, polys, out var polyCount, MaxPath);
        if (status.Failed() || polyCount == 0)
            return null;

        Span<DtStraightPath> straight = stackalloc DtStraightPath[MaxPath];
        if (query.FindStraightPath(startPt, endPt, polys, polyCount, straight, out var straightCount, MaxPath, 0).Failed())
            return null;

        var points = new List<Vector3>(straightCount);
        foreach (var p in straight[..straightCount])
            points.Add(new(p.pos.X, p.pos.Y, p.pos.Z));
        return new(points, startOver, endOver, status.IsPartial());
    }

    private void Clear()
    {
        _buildCts?.Cancel();
        _buildCts = null;
        _roomy = null;
        _tight = null;
    }

    private void TryStartBuild()
    {
        if (_buildCts is not null || !PlayerData.Available)
            return;

        var housing = HousingManager.Instance();
        var layout = LayoutWorld.Instance()->ActiveLayout;
        if (housing is null || !housing->IsInside() || layout is null || layout->InitState != 7)
            return;

        var parts = new List<Part>();
        CollectParts(LayoutWorld.Instance()->GlobalLayout, parts);
        CollectParts(layout, parts);

        var cts = _buildCts = new CancellationTokenSource();
        Task.Run(() => Build(parts, cts.Token), cts.Token);
    }

    private static void CollectParts(LayoutManager* layout, List<Part> parts)
    {
        if (layout is null)
            return;

        // Keyed lookups into this map are unreliable (see AnalyticShapeDataKey), so index it ourselves.
        var shapes = new Dictionary<uint, AnalyticShapeData>();
        foreach (var (k, v) in layout->CrcToAnalyticShapeData)
            shapes[k.Key] = v;

        if (layout->InstancesByType.TryGetValuePointer(InstanceType.BgPart, out var bgParts) && bgParts->Value is not null)
        {
            foreach (var (_, inst) in *bgParts->Value)
            {
                var bg = (BgPartsLayoutInstance*)inst.Value;
                if ((bg->Flags3 & 0x10) == 0)
                    continue;

                var world = inst.Value->GetTransformImpl()->Compose();
                if (bg->AnalyticShapeDataCrc != 0)
                {
                    if (!shapes.TryGetValue(bg->AnalyticShapeDataCrc, out var shape))
                        continue;

                    // Analytic shapes are unit shapes scaled to their bounds, see BgPartsLayoutInstance_calculateSRT.
                    var scale = (shape.BoundsMax - shape.BoundsMin) * 0.5f;
                    var type = (FileLayerGroupAnalyticCollider.Type)shape.Transform.Type;
                    if (type is FileLayerGroupAnalyticCollider.Type.Cylinder)
                        scale.Z = scale.X;
                    var local = Matrix4x4.CreateScale(scale);
                    local.Translation = (shape.BoundsMin + shape.BoundsMax) * 0.5f;
                    var kind = type switch
                    {
                        FileLayerGroupAnalyticCollider.Type.Plane => Shape.Plane,
                        FileLayerGroupAnalyticCollider.Type.Cylinder => Shape.Cylinder,
                        _ => Shape.Box,
                    };
                    parts.Add(new(kind, string.Empty, local * shape.Transform.Compose() * world));
                }
                else if (bg->CollisionMeshPathCrc != 0)
                {
                    parts.Add(new(Shape.Mesh, PathOf(layout, bg->CollisionMeshPathCrc), world));
                }
            }
        }

        if (layout->InstancesByType.TryGetValuePointer(InstanceType.CollisionBox, out var boxes) && boxes->Value is not null)
        {
            foreach (var (_, inst) in *boxes->Value)
            {
                var box = (CollisionBoxLayoutInstance*)inst.Value;
                // Skip colliders that are only conditionally active.
                var material = ((ulong)box->MaterialIdHigh << 32) | box->MaterialIdLow;
                if ((box->Flags3 & 0x10) == 0 || (material & 0x410) == 0x400)
                    continue;

                var shape = box->TriggerBoxLayoutInstance.Type switch
                {
                    Layer.ColliderType.Mesh => Shape.Mesh,
                    Layer.ColliderType.Plane or Layer.ColliderType.PlaneTwoSided => Shape.Plane,
                    Layer.ColliderType.Cylinder => Shape.Cylinder,
                    _ => Shape.Box,
                };
                parts.Add(new(shape, shape is Shape.Mesh ? PathOf(layout, box->PcbPathCrc) : string.Empty, box->Transform.Compose()));
            }
        }
    }

    private static string PathOf(LayoutManager* layout, uint crc)
        => layout->CrcToPath.TryGetValuePointer(crc, out var path) && path->Value is not null ? path->Value->DataString : string.Empty;

    private void Build(List<Part> parts, CancellationToken token)
    {
        try
        {
            var timer = Stopwatch.StartNew();
            var verts = new List<float>();
            var faces = new List<int>();
            var meshes = new Dictionary<string, Mesh>();
            foreach (var part in parts)
            {
                token.ThrowIfCancellationRequested();
                var mesh = part.Shape switch
                {
                    Shape.Box => Box,
                    Shape.Plane => Plane,
                    Shape.Cylinder => Cylinder,
                    _ => LoadMesh(meshes, part.Path),
                };

                var start = verts.Count / 3;
                foreach (var v in mesh.Verts)
                {
                    var w = Vector3.Transform(v, part.World);
                    verts.Add(w.X); verts.Add(w.Y); verts.Add(w.Z);
                }
                foreach (var i in mesh.Tris)
                    faces.Add(start + i);
            }

            // Recast rounds the radius up to whole cells, so each cell size keeps it close to what we ask for.
            var geom = new RcSampleInputGeomProvider(verts, faces);
            var roomy = BuildNavmesh(geom, 0.25f, 0.5f);
            var tight = BuildNavmesh(geom, 0.12f, 0.35f); // 3 cells = 0.36
            if (token.IsCancellationRequested)
                return;
            if (roomy is null || tight is null)
            {
                Logger.LogWarning($"Housing navmesh build produced no mesh from {parts.Count} parts.");
                return;
            }

            _tight = new DtNavMeshQuery(tight);
            _roomy = new DtNavMeshQuery(roomy);
            Logger.LogDebug($"Built housing navmeshes from {parts.Count} parts ({faces.Count / 3} tris) in {timer.ElapsedMilliseconds}ms.");
        }
        catch (OperationCanceledException) { }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Failed to build housing navmesh: {ex}");
        }
    }

    /// <summary> Unit cylinder (radius 1, y -1..1), matching the analytic cylinder collider. </summary>
    private static Mesh BuildCylinder(int segments)
    {
        var verts = new Vector3[segments * 2 + 2];
        var tris = new List<int>();
        int bottom = segments * 2, top = segments * 2 + 1;
        verts[bottom] = new(0, -1, 0);
        verts[top] = new(0, 1, 0);
        for (var i = 0; i < segments; ++i)
        {
            var angle = MathF.Tau * i / segments;
            verts[i * 2] = new(MathF.Cos(angle), -1, MathF.Sin(angle));
            verts[i * 2 + 1] = new(MathF.Cos(angle), 1, MathF.Sin(angle));
            var j = (i + 1) % segments;
            tris.AddRange([i * 2, j * 2, i * 2 + 1, i * 2 + 1, j * 2, j * 2 + 1]); // side
            tris.AddRange([top, j * 2 + 1, i * 2 + 1]); // top, facing up
            tris.AddRange([bottom, i * 2, j * 2]); // bottom, facing down
        }
        return new(verts, tris.ToArray());
    }

    private static DtNavMesh? BuildNavmesh(RcSampleInputGeomProvider geom, float cellSize, float agentRadius)
    {
        const float cellHeight = 0.25f, agentHeight = 2f, agentClimb = 0.55f, agentSlope = 55f;
        const int vertsPerPoly = 6;
        var cfg = new RcConfig(RcPartition.WATERSHED, cellSize, cellHeight, agentSlope, agentHeight, agentRadius, agentClimb,
            8, 20, 12f, 1.3f, vertsPerPoly, 6f, 1f, true, true, true, new RcAreaModification(RcRecast.RC_WALKABLE_AREA), true);
        var result = new RcBuilder().Build(geom, new RcBuilderConfig(cfg, geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax()), false);

        var pmesh = result.Mesh;
        var dmesh = result.MeshDetail;
        // The default query filter only passes polys with a flag set.
        Array.Fill(pmesh.flags, 1, 0, pmesh.npolys);
        var data = DtNavMeshBuilder.CreateNavMeshData(new DtNavMeshCreateParams
        {
            verts = pmesh.verts,
            vertCount = pmesh.nverts,
            polys = pmesh.polys,
            polyAreas = pmesh.areas,
            polyFlags = pmesh.flags,
            polyCount = pmesh.npolys,
            nvp = pmesh.nvp,
            detailMeshes = dmesh.meshes,
            detailVerts = dmesh.verts,
            detailVertsCount = dmesh.nverts,
            detailTris = dmesh.tris,
            detailTriCount = dmesh.ntris,
            walkableHeight = agentHeight,
            walkableRadius = agentRadius,
            walkableClimb = agentClimb,
            bmin = pmesh.bmin,
            bmax = pmesh.bmax,
            cs = cellSize,
            ch = cellHeight,
            buildBvTree = true,
        });
        var navmesh = new DtNavMesh();
        return data is not null && navmesh.Init(data, vertsPerPoly, 0).Succeeded() ? navmesh : null;
    }

    /// <summary> Reads a pcb collision mesh, caching it as one is often placed many times. </summary>
    private static Mesh LoadMesh(Dictionary<string, Mesh> cache, string path)
    {
        if (cache.TryGetValue(path, out var cached))
            return cached;

        var verts = new List<Vector3>();
        var tris = new List<int>();
        var file = path.Length > 0 ? Svc.Data.GetFile(path) : null;
        if (file is not null && file.Data.Length > 0)
        {
            fixed (byte* data = &file.Data[0])
            {
                var header = (MeshPCB.FileHeader*)data;
                if (header->Version is 1 or 4)
                    AddNode(verts, tris, (MeshPCB.FileNode*)(header + 1));
            }
        }
        return cache[path] = new(verts.ToArray(), tris.ToArray());
    }

    private static void AddNode(List<Vector3> verts, List<int> tris, MeshPCB.FileNode* node)
    {
        if (node is null)
            return;

        var start = verts.Count;
        for (var i = 0; i < node->NumVertsRaw + node->NumVertsCompressed; ++i)
            verts.Add(node->Vertex(i));
        foreach (ref var p in node->Primitives)
        {
            tris.Add(start + p.V1);
            tris.Add(start + p.V2);
            tris.Add(start + p.V3);
        }
        AddNode(verts, tris, node->Child1);
        AddNode(verts, tris, node->Child2);
    }
}
