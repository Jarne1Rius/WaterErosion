﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class D16 : Grid
{
    private int m_KernelErosion = 0;
    private int m_KernelDirection = 0;
    private int m_SizeForShader = 0;

    public void SetParameters(ComputeShader shader, string pathToWrite, float abrasion, float solubility, float deepWaterCutOff, float speedFlow)
    {
        Shader = shader;
        Path = pathToWrite;
        Abrasion = abrasion;
        Solubility = solubility;
        DeepWaterCutoff = deepWaterCutOff;
        SpeedFlow = speedFlow;
    }
    public override void Init(List<List<AlgorithmTypeFlow.Point>> vectors)
    {
        Name = "D16";
        BoundSize = 2;
        base.Init(vectors);

        m_KernelErosion = Shader.FindKernel("Erosion");
        Shader.SetBuffer(m_KernelErosion, "DensField", WaterBuffer);
        Shader.SetBuffer(m_KernelErosion, "Position", PositionBuffer);
        Shader.SetBuffer(m_KernelErosion, "Cells", CellBuffer);
        Shader.SetBuffer(m_KernelErosion, "PositionWater", PositionWaterBuffer);

        m_KernelDirection = Shader.FindKernel("CalculateD16");
        Shader.SetBuffer(m_KernelDirection, "DensField", WaterBuffer);
        Shader.SetBuffer(m_KernelDirection, "PositionAll", PositionAllBuffer);
        Shader.SetBuffer(m_KernelDirection, "Cells", CellBuffer);
        m_SizeForShader = Size + BoundSize * 2;

        StartCoroutine(Erode());
        StartCoroutine(ShowDirectionsAndAmount());
    }

    protected override void UpdateFlow()
    {
        Shader.Dispatch(m_KernelDirection, m_SizeForShader, m_SizeForShader, 1);
        Shader.Dispatch(m_KernelErosion, m_SizeForShader, m_SizeForShader, 1);

        base.UpdateFlow();
    }


    IEnumerator Erode()
    {
        float time = 0;
        while (true)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            UpdateFlow();
            stopwatch.Stop();
            time += Time.deltaTime;
            DurationUpdate += stopwatch.Elapsed;
            if (time > 1)
            {
                Debug.Log(stopwatch.Elapsed + " - " + stopwatch.ElapsedMilliseconds);
                time--;
            }
            yield return null;
        }
    }
}