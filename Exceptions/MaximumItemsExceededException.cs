using System;

public class MaximumItemsExceededException : Exception
{
    public int? ExcelMaximumItems { get; set; }
    public int ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;

    public MaximumItemsExceededException(int? excelMaximumItems)
    {
        ExcelMaximumItems = excelMaximumItems;
        ErrorCode = 1;
        Message = $"E' possibile scaricare massimo {excelMaximumItems} elementi.";
    }
}
