using System.Globalization;
using Inventor;
using InventorDxfExporter.Core;
using IOPath = System.IO.Path;

namespace InventorDxfExporter.Services;

/// <summary>
/// Reads part metadata for BOM export.
/// </summary>
public sealed class PartInfoServiceImplementation : IPartInfoService
{
    private const string DesignTrackingProperties = "Design Tracking Properties";
    private const string UserDefinedProperties = "Inventor User Defined Properties";

    public PartInfo Read(PartDocument part)
    {
        ArgumentNullException.ThrowIfNull(part);

        return new PartInfo(
            GetPartNumber(part),
            GetDescriptionProp(part),
            GetThicknessCustomProp(part),
            GetMaterial(part));
    }

    private static string GetPartNumber(PartDocument part)
    {
        try
        {
            var value = Convert.ToString(
                part.PropertySets[DesignTrackingProperties]["Part Number"].Value,
                CultureInfo.CurrentCulture);
            return string.IsNullOrWhiteSpace(value)
                ? IOPath.GetFileNameWithoutExtension(part.DisplayName)
                : value;
        }
        catch
        {
            return IOPath.GetFileNameWithoutExtension(part.DisplayName);
        }
    }

    private static string GetThicknessCustomProp(PartDocument part)
    {
        try
        {
            return Convert.ToString(
                part.PropertySets[UserDefinedProperties]["Thickness"].Value,
                CultureInfo.CurrentCulture) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetDescriptionProp(PartDocument part)
    {
        try
        {
            return Convert.ToString(
                part.PropertySets[DesignTrackingProperties]["Description"].Value,
                CultureInfo.CurrentCulture) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetMaterial(PartDocument part)
    {
        try
        {
            return part.ActiveMaterial is not null
                ? part.ActiveMaterial.DisplayName
                : "Khong_Xac_Dinh";
        }
        catch
        {
            return "Khong_Xac_Dinh";
        }
    }
}
