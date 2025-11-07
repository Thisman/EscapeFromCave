using System.Collections.Generic;

namespace Tools.EntitiesImporter
{
    public abstract class BaseEntityTableParser
    {
        public abstract IEnumerable<IEntityTableData> Parse(string tableContent, string delimiter);
    }
}
