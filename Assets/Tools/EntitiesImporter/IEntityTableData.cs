using System.Collections.Generic;

public interface IEntityTableData
{
    IReadOnlyDictionary<string, string> Fields { get; }

    bool TryGetValue(string columnName, out string value);
}
