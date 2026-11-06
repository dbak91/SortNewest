namespace SortNewest;

internal enum PickerSortOrder
{
    ModifiedNewestFirst,
    ModifiedOldestFirst,
    FileNameAscending,
    FileNameDescending,
    FileTypeAscending,
    FileTypeDescending,
    SizeLargestFirst,
    SizeSmallestFirst
}

internal sealed record PickerSortOption(
    PickerSortOrder Order,
    string DisplayName,
    string PickerResourceName,
    string EnglishPickerLabel);

internal static class PickerSortOptions
{
    internal static PickerSortOption Default { get; } = new(
        PickerSortOrder.ModifiedNewestFirst,
        "Modified (newest first)",
        "sort_dimension_date_descending",
        "Modified (newest first)");

    internal static IReadOnlyList<PickerSortOption> All { get; } =
    [
        Default,
        new(PickerSortOrder.ModifiedOldestFirst, "Modified (oldest first)",
            "sort_dimension_date_ascending", "Modified (oldest first)"),
        new(PickerSortOrder.FileNameAscending, "File name (A to Z)",
            "sort_dimension_name_ascending", "File name (A to Z)"),
        new(PickerSortOrder.FileNameDescending, "File name (Z to A)",
            "sort_dimension_name_descending", "File name (Z to A)"),
        new(PickerSortOrder.FileTypeAscending, "Type (A to Z)",
            "sort_dimension_file_type_ascending", "Type (A to Z)"),
        new(PickerSortOrder.FileTypeDescending, "Type (Z to A)",
            "sort_dimension_file_type_descending", "Type (Z to A)"),
        new(PickerSortOrder.SizeLargestFirst, "Size (largest first)",
            "sort_dimension_size_descending", "Size (largest first)"),
        new(PickerSortOrder.SizeSmallestFirst, "Size (smallest first)",
            "sort_dimension_size_ascending", "Size (smallest first)")
    ];

    internal static PickerSortOption From(PickerSortOrder order) =>
        All.FirstOrDefault(option => option.Order == order) ?? Default;

    internal static int IndexOf(PickerSortOrder order)
    {
        for (int index = 0; index < All.Count; index++)
        {
            if (All[index].Order == order)
                return index;
        }

        return 0;
    }
}
