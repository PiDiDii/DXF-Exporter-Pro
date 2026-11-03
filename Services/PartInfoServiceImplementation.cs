using System.Globalization;
using Inventor;
using InventorDxfExporter.Core;
using IOPath = System.IO.Path;

namespace InventorDxfExporter.Services;

/// <summary>
/// V14-compatible part metadata reader/updater. This class intentionally does not
/// change the existing IPartInfoService or the surrounding service registrations.
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

    public PartInfo UpdateFromFlatPattern(PartDocument part, bool saveIfDirty)
    {
        ArgumentNullException.ThrowIfNull(part);

        var sheetMetal = part.ComponentDefinition as SheetMetalComponentDefinition;
        if (sheetMetal is not null)
        {
            // V14 reads the named Thickness parameter and interprets Inventor database
            // length units (centimeters) as millimeters by multiplying by 10.
            var thicknessParameter = sheetMetal.Parameters["Thickness"];
            var thicknessMm = Math.Round(System.Convert.ToDouble(thicknessParameter.Value, CultureInfo.InvariantCulture) * 10.0, 2);

            if (!sheetMetal.HasFlatPattern)
                sheetMetal.Unfold();

            var flatPattern = sheetMetal.FlatPattern;
            var flatWidthMm = Math.Round(flatPattern.Width * 10.0, 1);
            var flatLengthMm = Math.Round(flatPattern.Length * 10.0, 1);
            var description = string.Concat(
                FormatNumberVn(flatLengthMm), " x ",
                FormatNumberVn(flatWidthMm), " x ",
                FormatNumberVn(thicknessMm));

            part.PropertySets[DesignTrackingProperties]
                ["Description"].Value = description;

            var thicknessText = thicknessMm.ToString(CultureInfo.InvariantCulture) + " mm";
            var customProperties = part.PropertySets[UserDefinedProperties];
            try
            {
                customProperties["Thickness"].Value = thicknessText;
            }
            catch
            {
                // V14 creates the user-defined property when it does not yet exist.
                customProperties.Add(thicknessText, "Thickness");
            }
        }

        // V14 saves only if the part became dirty; saveIfDirty is the explicit opt-in
        // exposed by the existing service contract.
        if (saveIfDirty && part.Dirty)
            part.Save();

        return Read(part);
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

    private static string FormatNumberVn(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
}
