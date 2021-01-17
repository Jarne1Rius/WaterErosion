using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cell
{
    public int ID { get; private set; }
    public int IdPosition { get; private set; }

    public List<Cell> Neighbors { get; set; }
    private readonly int m_HeightCell = 0;
    private readonly int m_WidthCell = 0;
    public Vector3 StartPosition { get; private set; }


    public Cell(int id, int widthCell, int heightCell, Vector3 startPosition, int idPosition)
    {
        m_HeightCell = heightCell;
        m_WidthCell = widthCell;
        ID = id;
        StartPosition = startPosition;
        IdPosition = idPosition;
        Neighbors = new List<Cell>();
    }
    
    public Vector2 GetPosition()
    {
        return new Vector2(m_WidthCell, m_HeightCell);
    }
}
