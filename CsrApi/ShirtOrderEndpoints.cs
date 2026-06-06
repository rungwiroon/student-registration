using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CsrApi.Models;
using CsrApi.Services;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CsrApi;

public static class ShirtOrderEndpoints
{
    public static void MapShirtOrderEndpoints(this WebApplication app)
    {
        // POST /api/v1/school-shirt/order
        app.MapPost("/api/v1/school-shirt/order", async (
            HttpContext context,
            ISlipStorageService slipStorage,
            IGoogleSheetsService googleSheets) =>
        {
            var lineUserId = GetLineUserId(context);
            if (lineUserId is null)
            {
                return Results.Unauthorized();
            }

            var formDataResult = await ReadShirtOrderFormAsync(context.Request, context.RequestAborted);
            if (formDataResult.IsLeft)
            {
                return formDataResult.Match<IResult>(
                    Right: _ => Results.Ok(),
                    Left: ToErrorResult);
            }

            var formData = formDataResult.MatchUnsafe(data => data, _ => null)!;
            var order = formData.Order;

            // Save slip file
            var slipResult = await slipStorage.SaveAsync(formData.SlipFile, lineUserId, context.RequestAborted);
            if (slipResult.IsLeft)
            {
                return slipResult.Match<IResult>(
                    Right: _ => Results.Ok(),
                    Left: ToErrorResult);
            }

            var storedSlip = slipResult.Match(
                Right: data => data,
                Left: _ => throw new InvalidOperationException("Slip storage result was expected to be successful."));

            // Build order summary string
            var orderSummary = BuildOrderSummary(order.Items);
            var orderId = Guid.NewGuid().ToString("N")[..12];

            // Append to Google Sheets
            var sheetResult = await googleSheets.AppendOrderRowAsync(
                DateTime.UtcNow,
                order.LineDisplayName,
                order.StudentName,
                order.StudentNumber,
                order.GuardianPhone,
                orderSummary,
                order.TotalAmount,
                storedSlip.PublicUrl,
                "Pending",
                context.RequestAborted);

            // Log sheet errors but don't block the user
            sheetResult.IfLeft(err => Console.WriteLine($"[WARN] Google Sheets append failed: {err.Message}"));

            return Results.Ok(new
            {
                Message = "Order received",
                OrderId = orderId,
                SlipUrl = storedSlip.PublicUrl
            });
        });

        // GET /api/v1/school-shirt/slips/{ownerKey}/{fileName}
        // Public access: URL contains SHA256 hash of ownerKey (security through obscurity)
        app.MapGet("/api/v1/school-shirt/slips/{ownerKey}/{fileName}", async (
            HttpContext context,
            ISlipStorageService slipStorage,
            string ownerKey,
            string fileName) =>
        {
            var result = await slipStorage.OpenReadAsync(ownerKey, fileName, context.RequestAborted);
            if (result.IsLeft)
            {
                return result.Match<IResult>(
                    Right: _ => Results.Ok(),
                    Left: ToErrorResult);
            }

            var slip = result.Match(
                Right: data => data,
                Left: _ => throw new InvalidOperationException("Slip read result was expected to be successful."));

            context.Response.OnCompleted(() => slip.DisposeAsync().AsTask());
            return Results.Stream(slip.Stream, slip.ContentType);
        });
    }

    private static string? GetLineUserId(HttpContext context)
    {
        return context.Items["LineUserId"]?.ToString();
    }

    private static async Task<LanguageExt.Either<AppError, ShirtOrderFormData>> ReadShirtOrderFormAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return AppError.BadRequest("Order requires multipart/form-data.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var payload = form["payload"].ToString();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return AppError.BadRequest("Order payload is required.");
        }

        try
        {
            var order = JsonSerializer.Deserialize<ShirtOrderRequest>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (order is null)
            {
                return AppError.BadRequest("Order payload is invalid.");
            }

            var slipFile = form.Files.GetFile("proofOfPayment");
            if (slipFile is null || slipFile.Length <= 0)
            {
                return AppError.BadRequest("Proof of payment slip is required.");
            }

            return new ShirtOrderFormData(order, slipFile);
        }
        catch (JsonException ex)
        {
            return AppError.BadRequest($"Order payload is invalid JSON: {ex.Message}");
        }
    }

    private static string BuildOrderSummary(List<ShirtOrderItem> items)
    {
        if (items is null || items.Count == 0)
        {
            return string.Empty;
        }

        var byDesign = items
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.Design)
            .Select(g =>
            {
                var sizes = g.Select(i => $"{i.Size}={i.Quantity}");
                return $"แบบที่ {g.Key} ({string.Join(", ", sizes)})";
            });

        return string.Join("\n", byDesign);
    }

    private static IResult ToErrorResult(AppError error)
    {
        return error.StatusCode switch
        {
            StatusCodes.Status400BadRequest => Results.BadRequest(error.Message),
            StatusCodes.Status401Unauthorized => Results.Unauthorized(),
            StatusCodes.Status404NotFound => Results.NotFound(error.Message),
            _ => Results.StatusCode(error.StatusCode)
        };
    }
}

public sealed record ShirtOrderFormData(ShirtOrderRequest Order, IFormFile SlipFile);
