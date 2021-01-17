
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Grid : MonoBehaviour
{
    public struct Item
    {
        public float ErosionWater;
        public float Inflow;
        public float Outflow;
        public float Height;
        public int IdPosition;
        public Vector2 Direction;
        public float heightdiff;

        public Item(float erosionWater, float inflow, float outflow, Vector2 direction)
        {
            this.ErosionWater = erosionWater;
            this.Inflow = inflow;
            this.Outflow = outflow;
            this.Direction = direction;
            this.Height = 0;
            this.IdPosition = -1;
            this.heightdiff = 0;
        }
    }

    protected int AmountUpdate = 0;
    protected TimeSpan DurationUpdate = TimeSpan.Zero;

    private bool t;
    //Parameters
    public bool WriteToFile { get; set; }
    public int Size { get; set; }
    public int BoundSize { get; set; }
    private List<Cell> Cells;

    protected float[] LiquidField = null;
    private Vector3[] m_PosStructure = null;
    private Vector3[] m_PosStructureWater = null;
    protected Vector3[] PosStructureAll = null;

    protected Item[] Items = null;

    protected ComputeBuffer CellBuffer = null;
    protected ComputeBuffer WaterBuffer = null;
    protected ComputeBuffer PositionBuffer = null;
    protected ComputeBuffer PositionWaterBuffer = null;
    protected ComputeBuffer PositionAllBuffer = null;

    protected ComputeShader Shader = null;
    protected string Path = "Assets/Test.txt";
    protected float Abrasion = 0.08f;
    protected float Solubility = 1f;
    protected float DeepWaterCutoff = 0.002f;
    protected float SpeedFlow = 0.1f;

    protected string Name = "D8";

    private void Startup()
    {
        Items = new Item[(Size + BoundSize * 2) * (Size + BoundSize * 2)];
        LiquidField = new float[(Size + BoundSize * 2) * (Size + BoundSize * 2)];

        Shader.SetFloat("Solubility", Solubility);
        Shader.SetFloat("Abrasion", Abrasion);
        Shader.SetFloat("DeepWaterCutOff", DeepWaterCutoff);
        Shader.SetFloat("SpeedFlow", SpeedFlow);
        Shader.SetFloat("Size", Size + BoundSize * 2);
    }

    //Functions
    public virtual void Init(List<List<AlgorithmTypeFlow.Point>> vectors)
    {
        Startup();
        Cells = new List<Cell>((Size + BoundSize) * (Size + BoundSize));
        int count = 0;
        for (int i = 0; i < Size + BoundSize * 2; i++)
        {
            for (int k = 0; k < Size + BoundSize * 2; k++)
            {
                if (i < BoundSize
                    || i >= Size + BoundSize
                    || k < BoundSize
                    || k >= Size + BoundSize)
                {
                    Cells.Add(new Cell(count, k, i, new Vector3(0, 0, 0), -1));
                    Items[count].Height = -100;
                }
                else
                {
                    Cells.Add(new Cell(count, k, i, vectors[i - BoundSize][k - BoundSize].Position, vectors[i - BoundSize][k - BoundSize].Id));
                    Items[count].Height = Cells[(Cells.Count - 1)].StartPosition.y;
                }

                Items[count].IdPosition = Cells[(Cells.Count - 1)].IdPosition;
                count++;
            }
        }

        CreateHeightField();

        CellBuffer = new ComputeBuffer(Items.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Item)));
        WaterBuffer = new ComputeBuffer(LiquidField.Length, sizeof(float));
        PositionBuffer = new ComputeBuffer(m_PosStructure.Length, 12);
        PositionWaterBuffer = new ComputeBuffer(m_PosStructureWater.Length, 12);
        PositionAllBuffer = new ComputeBuffer(PosStructureAll.Length, 12);

        CellBuffer.SetData(Items);
        WaterBuffer.SetData(LiquidField);
        PositionBuffer.SetData(m_PosStructure);
        PositionWaterBuffer.SetData(m_PosStructureWater);
        PositionAllBuffer.SetData(PosStructureAll);

        float distance1 = (new Vector2(Cells[(Size + BoundSize * 2) * BoundSize + BoundSize + 1].StartPosition.x, Cells[(Size + BoundSize * 2) * BoundSize + BoundSize + 1].StartPosition.z) -
                           new Vector2(Cells[(Size + BoundSize * 2) * BoundSize + BoundSize + 2].StartPosition.x, Cells[(Size + BoundSize * 2) * BoundSize + BoundSize + 2].StartPosition.z)).magnitude;
        float distance2 = Mathf.Sqrt(distance1 * distance1 + distance1 * distance1);
        float distance3 = Mathf.Sqrt((distance1  + distance1) * (distance1 + distance1) + distance1 * distance1);
        Shader.SetFloat("D1", distance1);
        Shader.SetFloat("D2", distance2);
        Shader.SetFloat("D3", distance3);
    }

    public void CreateHeightField()
    {
        m_PosStructure = GetComponent<MeshFilter>().mesh.vertices;
        m_PosStructureWater = GetComponent<AlgorithmTypeFlow>().GetExtraMesh().mesh.vertices;

        PosStructureAll = new Vector3[(Size + BoundSize * 2) * (Size + BoundSize * 2)];
        for (int i = 0; i < Cells.Count; i++)
        {
            PosStructureAll[i] = Cells[i].StartPosition;
        }

    }

    public virtual void Update()
    {
        if (!t)
        {
            StartCoroutine(Erode());
            StartCoroutine(ShowDirectionsAndAmount());
        }
    }
    #region DensityField for future   
    //These function can be used in the future to spread the liquid depending on the density

    public void UpdateVelocityField()
    {
        if (LiquidField == null) return;
        //  AddSource(Size, ref LiquidField, ref m_DensityPreviousField, Time.deltaTime);
        // Swap(ref LiquidField, ref m_DensityPreviousField);
        //Diffuse(Size, 1, ref LiquidField, ref m_DensityPreviousField, 0.01f, Time.deltaTime);
        //Swap(ref LiquidField, ref m_DensityPreviousField);
        // Advect(Size + BoundSize * 2, 1, ref LiquidField, ref m_DensityPreviousField, ref m_VelocityField, Time.deltaTime);
    }

    private void AddSource(int size, ref float[] arrayList, ref float[] previousList, float dt)
    {
        for (int i = 0; i < arrayList.Length; i++)
        {
            arrayList[i] += dt * previousList[i];
        }
    }

    private void Diffuse(int size, int b, ref float[] arrayList, ref float[] previousList, float diff, float dt)
    {
        float a = dt * diff * size * size;
        //Not sure if this is only the 4 
        LinSolve(size, b, ref arrayList, ref previousList, a, 1 + 4 * a);
    }

    private void LinSolve(int size, int b, ref float[] x, ref float[] x0, float a, float c)
    {
        if (x == null) return;
        if (x0 == null) return;
        for (int k = 0; k < 20; k++)
        {
            foreach (Cell cell in Cells)
            {
                int i = (int)cell.GetPosition().x;
                int j = (int)cell.GetPosition().y;
                //Temporarly
                if (x.Length <= ID(i, j)) continue;
                if (x0.Length <= ID(i, j)) continue;
                if (cell.IdPosition == -1) continue;
                float total = 0;

                if (x[ID(i, j)] > 0.01f)
                {
                    List<int> neighHeight = new List<int>();
                    int index = ID(i, j);
                    int i0 = ID(i - 1, j);
                    int i1 = ID(i + 1, j);
                    int j0 = ID(i, j - 1);
                    int j1 = ID(i, j + 1);
                    float value = x[i0] + Items[i0].Height;
                    float valueStart = x[index] + Items[index].Height;
                    if (value < valueStart)
                    {
                        neighHeight.Add(i0);
                        total += valueStart - value;
                    }
                    value = x[i1] + Items[i1].Height;
                    if (value < valueStart)
                    {
                        neighHeight.Add(i1);
                        total += valueStart - value;
                    }
                    value = x[j0] + Items[j0].Height;
                    if (value < valueStart)
                    {
                        neighHeight.Add(j0);
                        total += valueStart - value;
                    }
                    value = x[j1] + Items[j1].Height;
                    if (value < valueStart)
                    {
                        neighHeight.Add(j1);
                        total += valueStart - value;
                    }

                    total /= neighHeight.Count;
                    float t = 0;
                    foreach (int i2 in neighHeight)
                    {
                        float dep = (total - (x[i2] + Items[i2].Height)) * Time.deltaTime * 0.01f;
                        x[i2] += dep;
                        t += dep;
                    }

                    x[index] -= t;
                }

                //x[ID(i, j)] = (x0[ID(i, j)] +
                //            a * (total)) /
                //          c;
            }
            //SetBnd(size, b, ref x);
        }
    }

    private void SetBnd(int n, int b, ref float[] x)
    {
        int i;
        int N = Size + BoundSize * 2;
        if (x.Length <= 0) return;
        for (i = 1; i < N - BoundSize; i++)
        {
            x[ID(0, i)] = b == 1 ? -x[ID(1, i)] : x[ID(1, i)];

            x[ID(N - BoundSize, i)] = b == 1 ? -x[ID(N - BoundSize * 2, i)] : x[ID(N - BoundSize * 2, i)];
            x[ID(i, 0)] = b == 2 ? -x[ID(i, 1)] : x[ID(i, 1)];
            x[ID(i, N - BoundSize)] = b == 2 ? -x[ID(i, N - BoundSize * 2)] : x[ID(i, N - BoundSize * 2)];
        }

        x[ID(0, 0)] = 0.5f * (x[ID(1, 0)] + x[ID(0, 1)]);
        x[ID(0, N - BoundSize)] = 0.5f * (x[ID(1, N - BoundSize)] + x[ID(0, N - BoundSize * 2)]);
        x[ID(N - BoundSize, 0)] = 0.5f * (x[ID(N - BoundSize * 2, 0)] + x[ID(N - BoundSize, 1)]);
        x[ID(N - BoundSize, N - BoundSize)] = 0.5f * (x[ID(N - BoundSize * 2, N - BoundSize)] + x[ID(N - BoundSize, N - BoundSize * 2)]);
    }

    private void Advect(int size, int b, ref List<float> d, ref List<float> d0, ref List<Vector3> v0, float dt)
    {
        if (d0.Count <= 0) return;
        if (d.Count <= 0) return;
        if (v0.Count <= 0) return;
        int i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;
        dt0 = dt * Size;
        for (int j = 1; j < size - BoundSize; j++)
        {
            for (int i = 1; i < size - BoundSize; i++)
            {
                x = i - dt0 * v0[ID(i, j)].x; y = j - dt0 * v0[ID(i, j)].y;
                if (x < 0.5f) x = 0.5f; if (x > Size + 0.5f) x = Size + 0.5f; i0 = (int)Math.Floor(x); i1 = i0 + 1;
                if (y < 0.5f) y = 0.5f; if (y > Size + 0.5f) y = Size + 0.5f; j0 = (int)Math.Floor(y); j1 = j0 + 1;
                s1 = x - i0;
                s0 = 1 - s1;
                t1 = y - j0;
                t0 = 1 - t1;
                d[ID(i, j)] = s0 * (t0 * d0[ID(i0, j0)] + t1 * d0[ID(i0, j1)]) +
                                s1 * (t0 * d0[ID(i1, j0)] + t1 * d0[ID(i1, j1)]);
            }
        }
        //SetBnd(size, b, ref d);
    }
    private void Swap<T>(ref T a, ref T b)
    {
        T temp = b;
        b = a;
        a = temp;
    }
    private void DepositAt(int x, int y, float w, ref Vector2[] erosion, ref float[] heightmap)
    {
        float delta = Time.deltaTime * w;
        erosion[ID(x, y)].y += delta;
        heightmap[ID(x, y)] += delta;
    }

    private void Deposit(int x, int y, float xf, float zf, float h, ref Vector2[] erosion, ref float[] heightmap)
    {
        DepositAt(x, y, (1 - xf) * (1 - zf), ref erosion, ref heightmap);
        DepositAt(x + 1, y, xf * (1 - zf), ref erosion, ref heightmap);
        DepositAt(x, y + 1, (1 - xf) * (zf), ref erosion, ref heightmap);
        DepositAt(x + 1, y + 1, (xf) * (zf), ref erosion, ref heightmap);
    }

    #endregion

    protected int ID(int x, int y)
    {
        return ExtraMath.GetIdOfCell(x, y, Size + BoundSize * 2);
    }


    protected virtual void UpdateFlow()
    {
        AmountUpdate++;
        PositionBuffer.GetData(m_PosStructure);
        PositionWaterBuffer.GetData(m_PosStructureWater);


        GetComponent<AlgorithmTypeFlow>().UpdatePos(ref m_PosStructure, ref m_PosStructureWater);
    }

    protected void OnDestroy()
    {
        if (!this.enabled) return;
        CellBuffer.Release();
        WaterBuffer.Release();
        PositionBuffer.Release();
        PositionWaterBuffer.Release();
        PositionAllBuffer.Release();
        if (WriteToFile)
            SendToFile();
    }

    private void SendToFile()
    {
        StreamWriter writer = new StreamWriter(Path, true);
        if (AmountUpdate == 0) AmountUpdate = 1;
        writer.WriteLine(System.DateTime.Now + " Area: " + ((Size) * (Size)).ToString() + " Name: " + Name + "\n\t time: " + DurationUpdate.Ticks / (float)(AmountUpdate));
        writer.Close();
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
            if (time > 1)
            {
                Debug.Log(stopwatch.Elapsed + " - " + stopwatch.ElapsedMilliseconds);
                time--;
            }

            yield return null;
        }
    }

    protected IEnumerator ShowDirectionsAndAmount()
    {
        t = true;
        //yield break;
        //while (true)
        {
            int i = 0;
            foreach (Cell cell in Cells)
            {
                if (cell.IdPosition != -1)
                {
                    Vector2 neVector2 = Items[cell.ID].Direction;
                    float dens = LiquidField[cell.ID];
                    // if (dens > 0.00001f)
                    {
                        //  neVector2 *= 0.1f;
                        //Debug.Log(neVector2);
                        //Debug.DrawRay(cell.StartPosition, new Vector3(cell.Direction.x, 0f, cell.Direction.z), Color.red, Time.deltaTime);
                        Debug.DrawRay(m_PosStructure[cell.IdPosition], new Vector3(neVector2.x, 0f, neVector2.y), Color.red, Time.deltaTime);
                        Debug.DrawRay(m_PosStructure[cell.IdPosition], new Vector3(0, dens, 0), Color.black, Time.deltaTime);
                        i++;
                        //yield return new WaitForSeconds(1f);
                    }
                }
            }
            yield return null;
        }
    }

    public void OnHit(int id, float amount)
    {
        for (int i = 0; i < Cells.Count; i++)
        {
            if (Cells[i].IdPosition == id)
            {
                CellBuffer.GetData(Items);
                WaterBuffer.GetData(LiquidField);
                LiquidField[Cells[i].ID] += amount;
                WaterBuffer.SetData(LiquidField);
            }
        }
    }

}
