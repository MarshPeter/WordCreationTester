using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Report
{
    public string? ReportTitle { get; set; }
    public required List<Section> Sections { get; set; }
}   

public class Section
{
    public required string SectionTitle { get; set; }
    public required string SectionContext { get; set; }
    public required string ParagraphContext { get; set; }
    public required string Content { get; set; }
    public required List<Section> Sections { get; set; }
    public TableData? TableData { get; set; }
}

public class TableData
{
    public required int RowCount { get; set; }
    public required int ColumnCount { get; set; }
    public required List<Cell> Cells { get; set; }
    public string? Caption { get; set; }
}

public class Cell
{
    public required string Content { get; set; }
    public required int Row { get; set; }
    public required int Column { get; set; }
    public required string FontWeight { get; set; }
}

