﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//using Microsoft.AnalysisServices.AdomdClient;

using Microsoft.AnalysisServices.AdomdClient;


using Excel = Microsoft.Office.Interop.Excel;
//using ADOTabular;
using ADOTabular;


namespace DaxStudio
{
    public partial class DaxStudioForm : Form
    {

        ADOTabularConnection _conn;
        ServerConnections _connections = new ServerConnections();

        private enum tvwMetadataImages
        {
            Table = 0,
            Column = 1,
            HiddenColumn = 2,
            Measure = 3,
            HiddenMeasure = 4,
            Folder = 5,
            Function = 6
        }
        private Excel.Application app;


        public DaxStudioForm()
        {
            InitializeComponent();
        }

        public Excel.Application Application
        {
            get { return app; }
            set { app = value; }
        }

        private void DaxQueryDiscardResults()
        {
            //TODO
            System.Windows.Forms.MessageBox.Show("Not Implemented");
        }

        private void DaxQueryTable()
        {
            Excel.Workbook excelWorkbook = app.ActiveWorkbook;

            // Create a new Sheet
            Excel.Worksheet excelSheet = (Excel.Worksheet)excelWorkbook.Sheets.Add(
                Type.Missing, excelWorkbook.Sheets.get_Item(excelWorkbook.Sheets.Count)
                , 1, Excel.XlSheetType.xlWorksheet);

            Excel.ListObject lo = excelSheet.ListObjects.AddEx(0
                //, "OLEDB;Provider=MSOLAP.5;Persist Security Info=True;Initial Catalog=Microsoft_SQLServer_AnalysisServices;Data Source=$Embedded$;MDX Compatibility=1;Safety Options=2;ConnectTo=11.0;MDX Missing Member Mode=Error;Optimize Response=3;Cell Error Mode=TextValue"
                , @"OLEDB;Provider=MSOLAP.5;Persist Security Info=True;Data Source=.\SQL2012TABULAR;MDX Compatibility=1;Safety Options=2;ConnectTo=11.0;MDX Missing Member Mode=Error;Optimize Response=3;Cell Error Mode=TextValue"
                , Type.Missing
                , Excel.XlYesNoGuess.xlGuess
                , excelSheet.Range["$A$3"]);
            lo.QueryTable.CommandType = Excel.XlCmdType.xlCmdDefault;
            lo.QueryTable.CommandText = GetTextToExecute();
            try
            {
                WriteOutputMessage(string.Format("{0} - Starting Query Table Refresh", DateTime.Now));
                lo.QueryTable.Refresh(false);
                WriteOutputMessage(string.Format("{0} - Query Table Refresh Complete", DateTime.Now));
            }
            catch (Exception ex)
            {
                WriteOutputError(ex.Message);
            }
        }

        private void DaxQueryStaticResult()
        {
            Excel.Workbook wb = app.ActiveWorkbook;
            string wrkbkPath = wb.FullName;
            string connStr = _connections.ConnectionString(toolStrip1cmboModel.Text);
            AdomdConnection conn = new AdomdConnection(connStr);
            conn.Open();
            AdomdCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;

            cmd.CommandText = GetTextToExecute();

            DataTable dt = new DataTable("DAXQuery");
            AdomdDataAdapter da = new AdomdDataAdapter(cmd);
            try
            {

                ClearOutput();
                WriteOutputMessage(string.Format("{0} - Query Started", DateTime.Now));
                DateTime queryBegin = DateTime.UtcNow;

                // run query
                da.Fill(dt);
                DateTime queryComplete = DateTime.UtcNow;
                WriteOutputMessage(string.Format("{0} - Query Complete ({1:mm\\:ss\\.fff})", DateTime.Now, queryComplete - queryBegin));

                // output results
                CopyDataTableToRange(dt, wb);
                DateTime resultsEnd = DateTime.UtcNow;
                WriteOutputMessage(string.Format("{0} - Results Sent to Excel ({1:mm\\:ss\\.fff})", DateTime.Now, resultsEnd - queryComplete));
            }
            catch (Exception ex)
            {
                WriteOutputError(ex.Message);
            }
        }

        private string GetTextToExecute()
        {
            // if text is selected try to execute that
            if (this.userControl12.daxEditor.SelectionLength == 0)
                return this.userControl12.daxEditor.Text;
            else
                return this.userControl12.daxEditor.SelectedText;
        }

        private void ClearOutput()
        {
            this.rtbOutput.Clear();
            this.rtbOutput.ForeColor = Color.Black;
        }

        private void WriteOutputMessage(string message)
        {
            this.rtbOutput.AppendText(message + "\n");
        }

        private void WriteOutputError(string message)
        {
            this.rtbOutput.ForeColor = Color.Red;
            this.rtbOutput.Text = message;
        }

        private void CopyDataTableToRange(DataTable dt, Excel.Workbook excelWorkbook)
        {

            //        // Calculate the final column letter
            string finalColLetter = string.Empty;
            string colCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int colCharsetLen = colCharset.Length;

            if (dt.Columns.Count > colCharsetLen)
            {
                finalColLetter = colCharset.Substring(
                    (dt.Columns.Count - 1) / colCharsetLen - 1, 1);
            }

            finalColLetter += colCharset.Substring(
                    (dt.Columns.Count - 1) % colCharsetLen, 1);

            // Create a new Sheet
            Excel.Worksheet excelSheet = (Excel.Worksheet)excelWorkbook.Sheets.Add(
                Type.Missing, excelWorkbook.Sheets.get_Item(excelWorkbook.Sheets.Count)
                , 1, Excel.XlSheetType.xlWorksheet);

            //excelSheet.Name = dt.TableName;

            // Fast data export to Excel
            string excelRange = string.Format("A1:{0}{1}",
                finalColLetter, dt.Rows.Count + 1);

            // copying an object array to Value2 means that there is only one
            // .Net to COM interop call
            excelSheet.get_Range(excelRange, Type.Missing).Value2 = dt.ToObjectArray();

            // Mark the first row as BOLD
            ((Excel.Range)excelSheet.Rows[1, Type.Missing]).Font.Bold = true;

        }

        private void DaxStudioForm_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    DaxQueryStaticResult();
                    break;

            }

        }

        private void tspExportMetadata_Click(object sender, EventArgs e)
        {
            /*
            Excel.Workbook excelWorkbook = app.ActiveWorkbook;

            // Create a new Sheet
            Excel.Worksheet excelSheet = (Excel.Worksheet)excelWorkbook.Sheets.Add(
                Type.Missing, excelWorkbook.Sheets.get_Item(excelWorkbook.Sheets.Count)
                , 1, Excel.XlSheetType.xlWorksheet);

            Microsoft.AnalysisServices.Server svr = new Microsoft.AnalysisServices.Server();

            svr.Connect("$embedded$");
            Microsoft.AnalysisServices.Database db = svr.Databases[0];

            //"Type`tTable`tColumn`tSource"
            // Foreach dimension loop through each attribute and output the source
            foreach (Microsoft.AnalysisServices.Dimension dim in db.Dimensions)
            {
                //#"Dimension: $($dim.Name)"
                string tableSrc = ((Microsoft.AnalysisServices.QueryBinding)db.Cubes["Model"].MeasureGroups[dim.ID].Partitions[0].Source).QueryDefinition
                //"TABLE`t$($dim.Name)`t`t$tsrc"
                foreach (Microsoft.AnalysisServices.DimensionAttribute att in dim.Attributes)
                {

                    if (att.Name != "RowNumber")  // ## don't show the internal RowNumber column
                    {
                        foreach (Microsoft.AnalysisServices.Binding col in att.KeyColumns)
                        {
                            if (col is Microsoft.AnalysisServices.ExpressionBinding)
                            {
                                //"CALCULATED COLUMN`t$($dim.Name)`t$($att.Name)`t$($col.source.Expression)"
                            }
                            else
                            {
                                //"COLUMN`t$($dim.Name)`t$($att.Name)`t$($col.source.ColumnID)"
                            }
                        }
                    }
                }
            }

            svr.Disconnect();
             */
        }

        private void DaxStudioForm_Load(object sender, EventArgs e)
        {
            this.userControl12.AllowDrop = true;
            this.userControl12.Drop += new System.Windows.DragEventHandler(userControl12_Drop);

            /* PTB - add the default server */
            Excel.Workbook wb = app.ActiveWorkbook;
            string wrkbkPath = wb.FullName;
            _connections.AddConnection("servername", "Current Excel WorkBook",  "Data Source=$embedded$;Location=" + wrkbkPath + ";");

            toolStrip1cmboModel.Items.Add(_connections[0].ModelName);
            toolStrip1cmboModel.SelectedIndex = 0;
            
            //PopulateConnectionMetadata();
            // managed by changing the 'server' source
            
            PopulateOutputOptions(tcbOutputTo);      

        }

        private void PopulateOutputOptions(ToolStripComboBox tcbOutputTo)
        {
            Excel.Workbook wb = app.ActiveWorkbook;
            tcbOutputTo.Items.Add("<Query Results Sheet>");
            foreach (Excel.Worksheet ws in wb.Worksheets)
            {
                tcbOutputTo.Items.Add(ws.Name);
            }
            tcbOutputTo.Items.Add("<New Sheet>");
        }

        void userControl12_Drop(object sender, System.Windows.DragEventArgs e)
        {
            userControl12.daxEditor.SelectedText = e.Data.ToString();
        }

        private void PopulateConnectionMetadata()
        {
            foreach (ADOTabularModel m in _conn.Database.Models)
            {
                TreeNode modelNode = this.tvwMetadata.Nodes.Add(m.Name,m.Name); //todo - add image index
                foreach (ADOTabularTable t in m.Tables)
                {
                    TreeNode tableNode = modelNode.Nodes.Add(t.Name, t.Name, (int)tvwMetadataImages.Table, (int)tvwMetadataImages.Table);
                    foreach(ADOTabularColumn c in t.Columns)
                    {
                        // add different icons for hidden columns/measures
                        int iImageID = 0;
                        if (c.Type == ADOTabularColumnType.Column)
                        {
                            if (c.IsVisible == true)
                            {  iImageID = (int)tvwMetadataImages.Column;} 
                            else
                            { iImageID = (int)tvwMetadataImages.HiddenColumn; } 
                        }
                        else
                        {
                            if (c.IsVisible == true)
                            { iImageID = (int)tvwMetadataImages.Measure; }
                            else
                            { iImageID = (int)tvwMetadataImages.HiddenMeasure; }
                        }

                            tableNode.Nodes.Add(c.Name, c.Caption,iImageID,iImageID);
                    }
                }
                modelNode.Expand();

                foreach (ADOTabularFunction f in _conn.Functions)
                {
                    TreeNode groupNode;
                    int groupIndex = -1;
                    groupIndex = tvwFunctions.Nodes.IndexOfKey(f.Group);
                    if (groupIndex == -1)
                    {
                        groupNode = tvwFunctions.Nodes.Add(f.Group, f.Group, (int)tvwMetadataImages.Folder, (int)tvwMetadataImages.Folder);
                    }
                    else
                    {
                        groupNode = tvwFunctions.Nodes[groupIndex];
                    }
                    groupNode.Nodes.Add(f.Signature, f.Name, (int)tvwMetadataImages.Function, (int)tvwMetadataImages.Function);

                }

            }
        }

        private void tvw_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(((TreeNode)e.Item).Name, DragDropEffects.Move);
        }

        private void elementHost1_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e)
        {

        }

        private void runStaticResultsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DaxQueryStaticResult();
        }

        private void runDsicardResultsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DaxQueryDiscardResults();
        }

        private void runQueryTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DaxQueryTable();
        }


        // ptb
        private void toolStripCmdModels_Click(object sender, EventArgs e)
        {
            // loads dialog to get servers
            ServerManager _servers = new ServerManager(_connections);
            _servers.ShowDialog();

            // PTB -- will have to clean and add (later)
            string _currentSelection = toolStrip1cmboModel.Text;
            toolStrip1cmboModel.Items.Clear();
            for (int i = 0; i < _connections.Length; i++)
                toolStrip1cmboModel.Items.Add(_connections[i].ModelName);

            toolStrip1cmboModel.SelectedItem = _currentSelection;
               
        }



        private void toolStrip1cmboModel_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            string _connString = _connections.ConnectionString(toolStrip1cmboModel.Text);
            tvwMetadata.Nodes.Clear();
            _conn = new ADOTabularConnection(_connString);
            PopulateConnectionMetadata();

        }

    }
}