﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Meta.Numerics.Data
{

    // Supported storage types: int, double, bool, datetime, string
    // Supported data types: continuous, count, ordinal, categorical
    // Supported addenda: iid, series, circular

    /// <summary>
    /// A modifyable array of data.
    /// </summary>
    public sealed partial class DataFrame : DataView
    {
        private DataFrame ()
        {
            this.columns = new List<DataList>();
            this.columnMap = new Dictionary<string, int>();
            this.map = new List<int>();
        }

        /// <summary>
        /// Initializes a new data frame with the columns specifed by the given headers.
        /// </summary>
        /// <param name="columnHeaders"></param>
        public DataFrame(params ColumnDefinition[] columnHeaders) : this()
        {
            if (columnHeaders == null) throw new ArgumentNullException(nameof(columnHeaders));
            foreach(ColumnDefinition header in columnHeaders)
            {
                AddColumn(header.CreateList(this));
            }
        }

        /// <summary>
        /// Initializes a new data frame with the given data lists as columns.
        /// </summary>
        /// <param name="columns">The data lists.</param>
        internal DataFrame(params DataList[] columns) : this((IList<DataList>) columns)
        {

        }

        /// <summary>
        /// Initializes a new data frame with the given sequence of data lists as columns.
        /// </summary>
        /// <param name="columns">An enumerable sequence of non-null data lists.</param>
        internal DataFrame(IEnumerable<DataList> columns) : this()
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            foreach (DataList column in columns)
            {
                AddColumn(column);
            }
        }

        /*
        /// <summary>
        /// Joins this data view to another data view on the given column.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public DataFrame Join (DataFrame other, string columnName)
        {
            int thisColumnIndex = this.GetColumnIndex(columnName);
            Type thisType = this.columns[thisColumnIndex].StorageType;
            int otherColumnIndex = other.GetColumnIndex(columnName);
            Type otherType = other.columns[otherColumnIndex].StorageType;

            // Form a lookup from the other table
            Dictionary<object, int> hash = new Dictionary<object, int>();
            for (int otherRowIndex = 0; otherRowIndex < other.Rows.Count; otherRowIndex++)
            {
                hash[other.columns[otherColumnIndex].GetItem(other.map[otherRowIndex])] = otherRowIndex;
            }

            // Construct the joined columns
            List<DataList> joinedColumns = new List<DataList>();
            for (int i = 0; i < this.columns.Count; i++)
            {
                DataList joinedColumn = DataList.Create(this.columns[i].Name, this.columns[i].StorageType);
                joinedColumns.Add(joinedColumn);
            }
            for (int j = 0; j < other.columns.Count; j++)
            {
                DataList joinedColumn = DataList.Create(other.columns[j].Name, other.columns[j].StorageType);
                joinedColumns.Add(joinedColumn);
            }

            // Populate the joined columns
            for (int thisRowIndex = 0; thisRowIndex < this.map.Count; thisRowIndex++)
            {
                object thisValue = this.columns[thisColumnIndex].GetItem(this.map[thisRowIndex]);
                int otherRowIndex;
                if (hash.TryGetValue(thisValue, out otherRowIndex))
                {
                    for (int i = 0; i < this.columns.Count; i++)
                    {
                        joinedColumns[i].AddItem(this.columns[i].GetItem(this.map[i]));
                    }
                    for (int j = 0; j < other.columns.Count; j++)
                    {
                        joinedColumns[this.columns.Count + j].AddItem(other.columns[j].GetItem(other.map[otherRowIndex]));
                    }
                }
            }

            DataFrame result = new DataFrame(joinedColumns);
            return (result);

        }
        */

        // IReadableDataList
        // IDataList : IReadableDataList
        // IReadableDataList<T> : IReadableDataList
        // IDataList<T> : IReadableDataList<T>, IDataList

        // DataView
        //    DataTable
        //    VirtualDataTable

        // DataColumn
        //    DataList
        //    VirtualDataColumn
        //    ComputedDataColumn

        /// <summary>
        /// Adds the given data list as a column to the data frame.
        /// </summary>
        /// <param name="column">The data list to add.</param>
        /// <returns>The index of the new column.</returns>
        public int AddColumn(DataList column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (this.columns.Count == 0) {
                for (int i = 0; i < column.Count; i++) {
                    this.map.Add(i);
                }
            } else {
                if (column.Count != map.Count) throw new DimensionMismatchException();
            }
            int columnCount = this.columns.Count;
            this.columns.Add(column);
            this.columnMap[column.Name] = columnCount;
            return (columnCount);
        }

        /// <summary>
        /// Adds the given column to the data frame.
        /// </summary>
        /// <param name="column">The column to add.</param>
        public void AddColumn(ColumnDefinition column) {
            if (column == null) throw new ArgumentNullException(nameof(column));
            AddColumn(column.CreateList(this));
        }

        /// <summary>
        /// Removes the column with the given index from the data frame.
        /// </summary>
        /// <param name="columnName">The name of the column to remove.</param>
        public void RemoveColumn (string columnName)
        {
            if (columnName == null) throw new ArgumentNullException(nameof(columnName));
            int columnIndex = GetColumnIndex(columnName);
            this.columns.RemoveAt(columnIndex);
            // Remove the name of the removed column from the index,
            // and fix the index values for the higher columns, which will have changed 
            bool removed = this.columnMap.Remove(columnName);
            Debug.Assert(removed);
            for (int index = columnIndex; index < columns.Count; index++)
            {
                columnMap[columns[index].Name] = index;
            }
        }
       
        /// <summary>
        /// Add a new row of data to the data frame.
        /// </summary>
        /// <param name="values">The values to add to each column.</param>
        public void AddRow(params object[] values)
        {
            AddRow((IReadOnlyList<object>) values);
        }
        
        /// <summary>
        /// Adds a new row of data to the frame.
        /// </summary>
        /// <typeparam name="T">The type of the data collection.</typeparam>
        /// <param name="values">The values to add to each column.</param>
        public void AddRow<T>(IReadOnlyList<T> values) {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Count != columns.Count) throw new DimensionMismatchException();
            int r = -1;
            for (int i = 0; i < values.Count; i++) {
                int previous_r = r;
                r = columns[i].AddItem(values[i]);
                if (previous_r > 0) Debug.Assert(r == previous_r);
            }
            map.Add(r);
        }

        /// <summary>
        /// Adds a new row of data to the data frame.
        /// </summary>
        /// <param name="values"></param>
        public void AddRow(IReadOnlyDictionary<string, object> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            int rowCount = map.Count;
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                DataList column = columns[columnIndex];
                // handle computed columns
                object value = values[column.Name];
                int rowIndex = column.AddItem(value);
                if (rowIndex != rowCount) throw new InvalidOperationException();
            }
            map.Add(rowCount);
        }


        // parse to integer -> int
        // parse to double -> double
        // parse to datetime -> datetime
        // parse to boolean -> boolean
        // parse to guid -> guid
        // leave as string        

    }

}
