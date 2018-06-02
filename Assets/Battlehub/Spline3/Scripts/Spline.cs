using System;
using UnityEngine;

namespace Battlehub.Spline3
{
    [ExecuteInEditMode]
    public class Spline : SplineBase<PointData>
    {
        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            if(!GetComponent<SplineRenderer>())
            {
                gameObject.AddComponent<SplineRenderer>();
            }
        }

        protected override JunctionBase CreateJunctionOverride()
        {
            GameObject go = new GameObject();
            go.name = "Junction";
            Junction junction = go.AddComponent<Junction>();
            go.transform.SetParent(transform.parent);
            return junction;
        }

        protected override SplineBase<PointData> CreateSplineOverride()
        {
            GameObject go = new GameObject();
            go.name = "Spline";
            Spline spline = go.AddComponent<Spline>();
            go.transform.SetParent(transform.parent);
            return spline;
        }

        protected override SplinePointBase<PointData> CreatePointOverride(Vector3 position)
        {
            GameObject go = new GameObject();
            go.name = "SplinePoint";
            SplinePoint splinePoint = go.AddComponent<SplinePoint>();
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            return splinePoint;
        }
    }
}

