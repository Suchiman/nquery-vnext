using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NQuery.Symbols
{
    public sealed class TableRelation
    {
        private readonly TableSymbol _parentTable;
        private readonly ReadOnlyCollection<ColumnSymbol> _parentColumns;
        private readonly TableSymbol _childTable;
        private readonly ReadOnlyCollection<ColumnSymbol> _childColumns;

        public TableRelation(TableSymbol parentTable, IList<ColumnSymbol> parentColumns, TableSymbol childTable, IList<ColumnSymbol> childColumns)
        {
            if (parentColumns == null)
                throw new ArgumentNullException("parentColumns");

            if (childColumns == null)
                throw new ArgumentNullException("childColumns");

            if (parentColumns.Count == 0)
                throw new ArgumentException(Resources.ParentColumnsMustContainAtLeastOneColumn, "parentColumns");

            if (childColumns.Count != parentColumns.Count)
                throw new ArgumentException(Resources.ChildColumnsMustHaveSameSizeAsParentColumns, "childColumns");

            if (parentColumns.Any(c => !parentTable.Columns.Contains(c)))
                throw new ArgumentException(Resources.AllParentColumnsMustBelongToSameTable, "parentColumns");

            if (childColumns.Any(c => !childTable.Columns.Contains(c)))
                throw new ArgumentException(Resources.AllChildColumnsMustBelongToSameTable, "childColumns");

            _parentTable = parentTable;
            _parentColumns = new ReadOnlyCollection<ColumnSymbol>(parentColumns);
            _childTable = childTable;
            _childColumns = new ReadOnlyCollection<ColumnSymbol>(childColumns);
        }

        public int ColumnCount
        {
            get { return _parentColumns.Count; }
        }

        public TableSymbol ParentTable
        {
            get { return _parentTable; }
        }

        public ReadOnlyCollection<ColumnSymbol> ParentColumns
        {
            get { return _parentColumns; }
        }

        public TableSymbol ChildTable
        {
            get { return _childTable; }
        }

        public ReadOnlyCollection<ColumnSymbol> ChildColumns
        {
            get { return _childColumns; }
        }
    }
}