using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class D8 : Grid
{
    private int m_KernelErosion = 0;
    private int m_KernelDirection = 0;
    private int m_SizeForShader = 0;

    public void SetParameters(ComputeShader shader, string pathToWrite, float abrasion, float solubility, float deepWaterCutOff, float speedFlow, bool showTime)
    {
        Shader = shader;
        Path = pathToWrite;
        Abrasion = abrasion;
        Solubility = solubility;
        DeepWaterCutoff = deepWaterCutOff;
        SpeedFlow = speedFlow;
        ShowTime = showTime;
    }
    public override void Init(List<List<AlgorithmTypeFlow.Point>> vectors)
    {

        Name = "D8";
        base.Init(vectors);

        m_KernelErosion = Shader.FindKernel("Erosion");
        Shader.SetBuffer(m_KernelErosion, "LiquidField", WaterBuffer);
        Shader.SetBuffer(m_KernelErosion, "Position", PositionBuffer);
        Shader.SetBuffer(m_KernelErosion, "Cells", CellBuffer);
        Shader.SetBuffer(m_KernelErosion, "PositionWater", PositionWaterBuffer);

        m_KernelDirection = Shader.FindKernel("CalculateD8");
        Shader.SetBuffer(m_KernelDirection, "LiquidField", WaterBuffer);
        Shader.SetBuffer(m_KernelDirection, "PositionAll", PositionAllBuffer);
        Shader.SetBuffer(m_KernelDirection, "Cells", CellBuffer);
        m_SizeForShader = Size + BoundSize * 2;

    }

    protected override void UpdateFlow()
    {
        Shader.Dispatch(m_KernelDirection, m_SizeForShader, m_SizeForShader, 1);
        Shader.Dispatch(m_KernelErosion, m_SizeForShader, m_SizeForShader, 1);

        base.UpdateFlow();
    }


    public override IEnumerator Erode()
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
                if (ShowTime)
                    Debug.Log(stopwatch.Elapsed + " - " + stopwatch.ElapsedMilliseconds);
                time--;
            }
            yield return null;
        }
    }
}
