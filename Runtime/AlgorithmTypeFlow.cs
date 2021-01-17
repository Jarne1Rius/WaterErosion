using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlgorithmTypeFlow : MonoBehaviour
{
    enum TypeOfAlgorithm
    {
        D4, D8, D16, FD8, DInfinite
    }
    public struct Point
    {
        public Point(int id, Vector3 position)
        {
            Id = id;
            Position = position;
        }
        public readonly int Id;
        public Vector3 Position;
    }

    [SerializeField] private TypeOfAlgorithm m_TypeOfAlgorithm;
    [SerializeField] private MeshFilter m_WaterMesh = null;
    [SerializeField] private bool m_ApplyTransformMatrix = false;
    [SerializeField] private bool m_WriteTimeToFile = false;

    [Header("Algorithm parameters")]
    [SerializeField] protected ComputeShader m_Shader;
    [SerializeField] protected string m_Path = "Assets/Test.txt";
    [SerializeField] protected float m_Abrasion = 0.001f;
    [SerializeField] protected float m_Solubility = 0.001f;
    [SerializeField] protected float m_DeepWaterCutoff = 0.01f;
    [SerializeField] protected float m_SpeedFlow = 30.0f;
    private MeshFilter m_SurfaceMesh;
    private readonly List<List<Point>> m_VectorPos = new List<List<Point>>();

    public MeshFilter GetExtraMesh()
    {
        return m_WaterMesh;
    }

    public List<List<Point>> GetVectorPos()
    {
        return m_VectorPos;
    }


    private void Start()
    {
        m_SurfaceMesh = GetComponent<MeshFilter>();
        SetVectors();
        SetAlgorithm();
        Grid newgrid = GetComponent<Grid>();

        newgrid.WriteToFile = m_WriteTimeToFile;
        newgrid.Size = m_VectorPos.Count;
        newgrid.BoundSize = 1;
        newgrid.Init(m_VectorPos);
    }

    private void SetAlgorithm()
    {
        switch (m_TypeOfAlgorithm)
        {
            case TypeOfAlgorithm.D8:
                D8 d8 = gameObject.AddComponent<D8>();
                d8.SetParameters(m_Shader, m_Path, m_Abrasion, m_Solubility, m_DeepWaterCutoff, m_SpeedFlow);
                break;
            case TypeOfAlgorithm.D4:
                D4 d4 = gameObject.AddComponent<D4>();
                d4.SetParameters(m_Shader, m_Path, m_Abrasion, m_Solubility, m_DeepWaterCutoff, m_SpeedFlow);
                break;
            case TypeOfAlgorithm.D16:
                D16 d16 = gameObject.AddComponent<D16>();
                d16.SetParameters(m_Shader, m_Path, m_Abrasion, m_Solubility, m_DeepWaterCutoff, m_SpeedFlow);
                break;
            case TypeOfAlgorithm.FD8:
                FD8 fd8 = gameObject.AddComponent<FD8>();
                fd8.SetParameters(m_Shader, m_Path, m_Abrasion, m_Solubility, m_DeepWaterCutoff, m_SpeedFlow);
                break;
            case TypeOfAlgorithm.DInfinite:
                DInfinite dInfinite = gameObject.AddComponent<DInfinite>();
                dInfinite.SetParameters(m_Shader, m_Path, m_Abrasion, m_Solubility, m_DeepWaterCutoff, m_SpeedFlow);

                break;
        }
    }

    private void SetVectors()
    {
        int count = 0;
        foreach (Vector3 meshVertex in m_SurfaceMesh.sharedMesh.vertices)
        {
            int i;
            for (i = 0; i < m_VectorPos.Count; i++)
            {
                if (m_ApplyTransformMatrix)
                {
                    Vector3 p = transform.localToWorldMatrix * new Vector4(meshVertex.x, meshVertex.y, meshVertex.z, 1);
                    if (Mathf.Abs(m_VectorPos[i][0].Position.x - p.x) < 0.001f)
                    {
                        m_VectorPos[i].Add(new Point(count, p));
                        break;
                    }
                }
                else
                {
                    if (Mathf.Abs(m_VectorPos[i][0].Position.x - meshVertex.x) < 0.001f)
                    {
                        m_VectorPos[i].Add(new Point(count, meshVertex));
                        break;
                    }
                }

            }

            if (i >= m_VectorPos.Count)
            {
                m_VectorPos.Add(new List<Point>());
                if (m_ApplyTransformMatrix)
                {
                    Vector3 p = transform.worldToLocalMatrix * new Vector4(meshVertex.x, meshVertex.y, meshVertex.z, 1);
                    m_VectorPos[m_VectorPos.Count - 1].Add(new Point(count, p));
                }
                else
                {
                    m_VectorPos[m_VectorPos.Count - 1].Add(new Point(count, meshVertex));
                }
            }

            count++;
        }

        m_VectorPos.Sort((p1, p2) => p1[0].Position.x.CompareTo(p2[0].Position.x));
        foreach (List<Point> vector3s in m_VectorPos)
        {
            vector3s.Sort((p1, p2) => p1.Position.z.CompareTo(p2.Position.z));
        }
    }


    public void UpdatePos(ref Vector3[] pos, ref Vector3[] waterPos)
    {
        m_SurfaceMesh.mesh.vertices = pos;
        m_SurfaceMesh.mesh.MarkModified();
        // GetComponent<MeshCollider>().sharedMesh = m_SurfaceMesh.mesh;
        m_WaterMesh.mesh.vertices = waterPos;
        m_WaterMesh.mesh.MarkModified();
    }
}
