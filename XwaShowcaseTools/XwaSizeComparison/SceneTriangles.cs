using JeremyAnsel.DirectX.DXMath;
using System.Collections.Generic;

namespace XwaSizeComparison
{
    static class SceneTriangles
    {
        public static void Build(XMUInt4[] indices, XMVector[] centers, in XMMatrix m)
        {
            var triangles = new List<SceneTriangle>(centers.Length);

            for (int i = 0; i < centers.Length; i++)
            {
                var triangle = new SceneTriangle
                {
                    Index0 = indices[i * 3 + 0],
                    Index1 = indices[i * 3 + 1],
                    Index2 = indices[i * 3 + 2],
                    Center = centers[i]
                };

                triangle.ComputeDepth(m);

                triangles.Add(triangle);
            }

            Quicksort.Sort(triangles);

            for (int i = 0; i < centers.Length; i++)
            {
                indices[i * 3 + 0] = triangles[i].Index0;
                indices[i * 3 + 1] = triangles[i].Index1;
                indices[i * 3 + 2] = triangles[i].Index2;
                centers[i] = triangles[i].Center;
            }
        }
    }
}
