﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Archiver {
    public partial class ArchiverMainWindow : Form {

        private SearchFilter searchFilter;

        private List<FileData> fileList = new List<FileData>();
        private string parentFolder;
        private SortOrder sortOrder = SortOrder.Ascending;

        private void ArchiverMainWindow_Shown(object sender, EventArgs e) {
            cbxSearchStyle.SelectedIndex = 0;
        }

        private void ArchiverMainWindow_FormClosing(object sender, FormClosingEventArgs e) {
            SaveColumnWidths();
        }

        private void btnScan_Click(object sender, EventArgs e) {
            BuildNewFileList();
            LoadItemsFromFileList();
        }

        private void btnRefresh_Click(object sender, EventArgs e) {
            fileList = new List<FileData>();
            RefreshDataGridView();
        }

        private void dataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            sortOrder = sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            var fileDataComparer = new FileDataComparer((ColumnType)e.ColumnIndex, sortOrder);
            fileList.Sort(fileDataComparer);
            dataGridView.Refresh();
        }

        private void dateTimePicker_ValueChanged(object sender, EventArgs e) {
            btnRefresh.Enabled = true;
        }

        private void radOlderThan_CheckedChanged(object sender, EventArgs e) {
            searchFilter.Period = GetSearchPeriod();
            Debug.WriteLine(dataGridView.Columns[0].Width);
            Debug.WriteLine(Properties.Settings.Default.colFileWidth);
        }

        private void chkFilter_CheckedChanged(object sender, EventArgs e) {
            EnableFilterControls(chkFilter.Checked);
        }

        private void EnableFilterControls(bool condition) {
            cbxSearchStyle.Enabled = condition;
            radOlderThan.Enabled = condition;
            radNewerThan.Enabled = condition;
            dateTimePicker.Enabled = condition;
        }

        private void cbxSearchStyle_SelectedIndexChanged(object sender, EventArgs e) {
            searchFilter.Style = GetSearchStyle();
            btnRefresh.Enabled = DataListHasItems();
        }

        private void contextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if (dataGridView.SelectedRows.Count <= 0) return;
            var name = (string) dataGridView.SelectedRows[0].Cells["File"].Value;
            var path = (string) dataGridView.SelectedRows[0].Cells["Path"].Value;
            if (e.ClickedItem == ctxOpenFileLocation) {
                Process.Start("explorer.exe", path);
            }
            else if (e.ClickedItem == ctxOpenFile) {
                var pathToFile = path + "\\" + name;
                Process.Start("explorer.exe", pathToFile);
            }
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e) {
            e.Cancel = dataGridView.SelectedRows.Count <= 0;
        }

        private void RefreshDataGridView() {
            SetSearchFilter();
            BuildFileListFromParentFolder();
            LoadItemsFromFileList();
        }

        private void BuildDataGridViewColumns() {
            dataGridView.Columns.Clear();
            dataGridView.AutoGenerateColumns = false;
            colFile = new DataGridViewTextBoxColumn {
                DataPropertyName = "Name",
                Name = "File",
                Width = Properties.Settings.Default.colFileWidth
            };
            colExtension = new DataGridViewTextBoxColumn {
                DataPropertyName = "Extension",
                Name = "Extension",
                Width = Properties.Settings.Default.colExtWidth
            };
            colSize = new DataGridViewTextBoxColumn {
                DataPropertyName = "Size",
                Name = "Size",
                Width = Properties.Settings.Default.colSizeWidth
            };
            colDateModified = new DataGridViewTextBoxColumn {
                DataPropertyName = "DateModified",
                Name = "Date Modified",
                Width = Properties.Settings.Default.colDateModWidth
            };
            colDateAccessed = new DataGridViewTextBoxColumn {
                DataPropertyName = "DateAccessed",
                Name = "Date Accessed",
                Width = Properties.Settings.Default.colDateAccWidth
            };
            colDateCreated = new DataGridViewTextBoxColumn {
                DataPropertyName = "DateCreated",
                Name = "Date Created",
                Width = Properties.Settings.Default.colDateCreateWidth
            };
            colPath = new DataGridViewTextBoxColumn {
                DataPropertyName = "Path",
                Name = "Path",
                Width = Properties.Settings.Default.colPathWidth
            };
            dataGridView.Columns.AddRange(colFile, colExtension, colSize, colDateModified, colDateAccessed,
                colDateCreated, colPath);
        }

        private bool DataListHasItems() {
            return dataGridView.RowCount > 0;
        }

        private string SetParentFolder() {
            folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return null;
            var path = folderBrowserDialog.SelectedPath;
            SetWindowTitle(path);
            return path;
        }

        private void SetWindowTitle(string title) {
            Text = title + @" - Archiver";
        }

        private void SaveColumnWidths() {
            Properties.Settings.Default.colFileWidth = dataGridView.Columns[0].Width;
            Properties.Settings.Default.colExtWidth = dataGridView.Columns[1].Width;
            Properties.Settings.Default.colSizeWidth = dataGridView.Columns[2].Width;
            Properties.Settings.Default.colDateModWidth = dataGridView.Columns[3].Width;
            Properties.Settings.Default.colDateAccWidth = dataGridView.Columns[4].Width;
            Properties.Settings.Default.colDateCreateWidth = dataGridView.Columns[5].Width;
            Properties.Settings.Default.colPathWidth = dataGridView.Columns[6].Width;
            Properties.Settings.Default.Save();
        }

        private void SetSearchFilter() {
            searchFilter = new SearchFilter {
                Style = GetSearchStyle(),
                Period = GetSearchPeriod(),
                Date = dateTimePicker.Value,
                Option = GetSearchOption(),
                Enabled = chkFilter.Checked
            };
        }

        private SearchStyle GetSearchStyle() {
            return (SearchStyle) cbxSearchStyle.SelectedIndex;
        }

        private SearchPeriod GetSearchPeriod() {
            return radOlderThan.Checked ? SearchPeriod.OlderThan : SearchPeriod.NewerThan;
        }

        private SearchOption GetSearchOption() {
            return chkIncludeSubDirs.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        }

        private void BuildNewFileList() {
            var tryParentFolder = SetParentFolder();
            if (tryParentFolder == null) return;
            parentFolder = tryParentFolder;
            SetSearchFilter();
            BuildFileListFromParentFolder();
        }

        private void BuildFileListFromParentFolder() {
            var getFileListForm = new GetFileListForm(parentFolder, searchFilter);
            if (getFileListForm.ShowDialog() != DialogResult.OK || getFileListForm.fileList.Count <= 0) return;
            fileList = getFileListForm.fileList;
        }

        private void LoadItemsFromFileList() {
            dataGridView.DataSource = null;
            BuildDataGridViewColumns();
            if (fileList.Count <= 0) {
                dataGridView.Rows.Add("No matching files.", "", "", "", "", "", "");
                Beep();
                return;
            }
            dataGridView.DataSource = fileList;
            btnRefresh.Enabled = true; // TODO TEMP
            tslblFileCount.Text = @"File count: " + fileList.Count; // TODO TEMP
        }

        private static void Beep() {
            System.Media.SystemSounds.Asterisk.Play();
        }

        public ArchiverMainWindow() {
            InitializeComponent();
            searchFilter = new SearchFilter();
        }

 
    }
}