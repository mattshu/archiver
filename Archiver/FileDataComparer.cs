﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Archiver {
    internal class FileDataComparer : IComparer<FileData> {
        public FileDataComparer(ColumnType column, SortOrder order) {
            Column = column;
            Order = order;
        }

        public ColumnType Column { get; set; }
        public SortOrder Order { get; set; }

        public int Compare(FileData itemX, FileData itemY) {
            if (itemX == null && itemY == null)
                return 0;
            if (itemX == null)
                return -1;
            if (itemY == null)
                return 1;
            if (itemX == itemY)
                return 0;

            int value;

            switch (Column) {
                case ColumnType.Name:
                    value = string.CompareOrdinal(itemX.Name, itemY.Name);
                    break;
                case ColumnType.Extension:
                    value = string.CompareOrdinal(itemX.Extension, itemY.Extension);
                    break;
                case ColumnType.Size:
                    value = CompareAsSize(itemX, itemY);
                    break;
                case ColumnType.DateModified:
                    value = DateTime.Compare(itemX.DateModified, itemY.DateModified);
                    break;
                case ColumnType.DateAccessed:
                    value = DateTime.Compare(itemX.DateAccessed, itemY.DateAccessed);
                    break;
                case ColumnType.DateCreated:
                    value = DateTime.Compare(itemX.DateCreated, itemY.DateCreated);
                    break;
                case ColumnType.Path:
                    value = decimal.Compare(itemX.Path.TakeWhile(c => c == '\\').Count(),
                        itemY.Path.TakeWhile(c => c == '\\').Count());
                    break;
                default:
                    return 0;
            }

            if (Order == SortOrder.Descending) value *= -1;
            return value;
        }

        public int CompareAsSize(FileData itemX, FileData itemY) {
            var itemXSize = GetSize(itemX);
            var itemYSize = GetSize(itemY);
            return decimal.Compare(itemXSize, itemYSize);
        }

        private static decimal GetSize(FileData data) {
            var splitText = data.Size.Split();
            if (splitText.Length <= 1) return -1;
            var size = decimal.Parse(splitText[0]);
            var sizeSuffix = splitText[1];
            switch (sizeSuffix) {
                case "MB":
                    size *= 1024;
                    break;
                case "GB":
                    size *= 1024 * 1024;
                    break;
                case "TB":
                    size *= 1024 * 1024 * 1024;
                    break;
                default:
                    break;
            }
            return size;
        }
    }
}