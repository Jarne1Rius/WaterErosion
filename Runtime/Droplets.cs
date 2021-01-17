using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class Droplets : MonoBehaviour
{
    [SerializeField] private LayerMask m_Mask = 0;
    [SerializeField] private float m_Radius = 0.1f;
    [SerializeField] private float m_Speed = 0.1f;
    private bool t;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A)) t = !t;
        if (!t) return;
        Vector3 newpos = Random.insideUnitCircle * m_Radius;
        newpos.z = newpos.y;
        newpos.y = 0;
        newpos += transform.position;
        Ray ray = new Ray(newpos, Vector3.down);
        RaycastHit info = new RaycastHit();
        if (Physics.Raycast(ray, out info, 100, m_Mask))
        {
            if (info.transform.GetComponent<Grid>())
            {
                int closestX = 0;
                int closestY = 0;
                List<List<AlgorithmTypeFlow.Point>> points = info.transform.GetComponent<AlgorithmTypeFlow>().GetVectorPos();
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i][0].Position.x < info.point.x) closestX = i;
                    for (int j = 0; j < points[i].Count; j++)
                    {
                        if (points[i][j].Position.z < info.point.z) closestY = j;
                    }
                }
                info.transform.GetComponent<Grid>().OnHit(points[closestX][closestY].Id,m_Speed);

            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, m_Radius);
    }
}
