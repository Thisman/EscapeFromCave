using System.Collections.Generic;

namespace Tools.EntitiesImporter
{
    public interface IEntityTableData
    {
        IReadOnlyDictionary<string, string> Fields { get; }

        bool TryGetValue(string columnName, out string value);
    }
}
