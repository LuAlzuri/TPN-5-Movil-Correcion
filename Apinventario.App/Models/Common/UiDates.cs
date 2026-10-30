namespace Apinventario.App.Models.Common;
public static class UiDates
{
    // SQL datetime2 values from this API are UTC even when JSON omits a timezone suffix.
    public static DateTime Local(DateTime value) => value.Kind == DateTimeKind.Local ? value :
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
}
