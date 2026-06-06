using System;
using System.Collections.Generic;

namespace CsrApi.Models;

public class ShirtOrderRequest
{
    public string LineDisplayName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string GuardianPhone { get; set; } = string.Empty;
    public List<ShirtOrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class ShirtOrderItem
{
    public string Design { get; set; } = string.Empty; // "A" or "B"
    public string Size { get; set; } = string.Empty;   // "XS" to "7XL"
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
