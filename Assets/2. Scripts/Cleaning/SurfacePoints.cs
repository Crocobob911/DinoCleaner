using System.Collections.Generic;
using UnityEngine;

namespace DinoCleaner.Cleaning
{
    // 렌더러들의 vertices를 월드좌표(위치 + 법선(필요시))로 모아 둔다. 오물을 뿌릴 위치를 고를 때 사용.
    internal sealed class SurfacePoints
    {
        readonly List<Vector3> _positions = new List<Vector3>();
        // readonly List<Vector3> _normals = new List<Vector3>();

        public int Count => _positions.Count;

        public SurfacePoints(Renderer[] renderers)
        {
            var baked = new Mesh();   // skinned mesh를 구울 임시 mesh

            foreach (var r in renderers)
            {
                Mesh mesh;
                Matrix4x4 toWorld;

                if (r is SkinnedMeshRenderer skinned)
                {
                    // 뼈가 있는 메시는 지금 포즈 그대로 일반 메시로 구워서 읽는다
                    skinned.BakeMesh(baked, true);   // true = 스케일까지 적용해서 굽기
                    mesh = baked;
                    toWorld = Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
                }
                else if (r.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    mesh = filter.sharedMesh;
                    toWorld = r.transform.localToWorldMatrix;
                }
                else continue;   // mesh없는 렌더러(파티클 등)는 건너뜀

                if (!mesh.isReadable)
                {
                    Debug.LogWarning($"[SurfacePoints] {mesh.name}: 모델 Import 설정에서 Read/Write를 켜야 오물을 뿌리기 가능!!", r);
                    continue;
                }

                var vertices = mesh.vertices;
                var normals = mesh.normals;
                // bool hasNormals = normals.Length == vertices.Length;

                for (int i = 0; i < vertices.Length; i++)
                {
                    _positions.Add(toWorld.MultiplyPoint3x4(vertices[i]));
                   // _normals.Add(hasNormals ? toWorld.MultiplyVector(normals[i]).normalized : Vector3.up);
                }
            }

            Object.Destroy(baked);
        }

        // 정점 하나를 무작위로 고른다. 정점이 없으면 false
        // TODO: 정점이 많으면 랜덤 선택이 느려질 수 있으므로, 필요하면 샘플링해서 리스트를 줄이는 방법 고려
        // TODO: 정점이 몰리면 확률이 높아지므로 면적 비례 선택방법 고려
        // TODO: 발바닥 같은 곳은 청소 힘드니까, 법선 방향따라 선별 선택하는 옵션 고려
        public bool TryPickRandom(out Vector3 position)
        {
            position = default;
            if (_positions.Count == 0) return false;

            position = _positions[Random.Range(0, _positions.Count)];
            return true;
        }
    }
}
