// ================================================
//
// SPDX-FileCopyrightText: 2024/25 Stefan Warnke
//
// SPDX-License-Identifier: BeerWare
//
//=================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MicroCodeGenerator
{
    /// <summary>
    /// Main form of the Micro Code Generator.
    /// </summary>
    public partial class frmMain : Form
    {
        #region Private Constants
        /// <summary>Number of lines per instruction in the grid view.</summary>
        private const int LINES_PER_INSTRUCTION = 6;
        /// <summary>Number of possible "instruction" fields for covering the Exceptions.</summary>
        private const int NO_OF_EXCEPTION_FIELDS = 8;
        /// <summary>File name for the lower 8 bit of the micro code ROM.</summary>
        private const string ROM_LOW_FILENAME = "ROM_L.bin";
        /// <summary>File name for the upper 8 bit of the micro code ROM.</summary>
        private const string ROM_HIGH_FILENAME = "ROM_H.bin";
        /// <summary>File name for the comments loaded to the grid view for all micro steps.</summary>
        private const string ROM_TEXT_FILENAME = "ROM_Texts.prn";
        /// <summary>File name for the histogram output of all codes.</summary>
        private const string ROM_HIST_FILENAME = "ROM_Histograms.csv";
        /// <summary>File name of the structured text file containing the mnemonics and additional information.</summary>
        private const string CPU_OPCODE_FILENAME = "CPU_6502_Opcodes.txt";
        /// <summary>File name to dump grid contents to.</summary>
        private const string GRID_CSV_FILENAME = "ROM_Grid_Section_";
        /// <summary>Sub directory to keep the ROm data.</summary>
        private const string ROM_DIR = "\\ROM\\";
        /// <summary>Sub directory to store logs and outputs.</summary>
        private const string OUTPUT_DIR = "\\Outputs\\";

        /// <summary>Column definition for the opcode in the structured CPU opcode text file.</summary>
        private const int COL_OPCODE = 0;
        /// <summary>Column definition for the mnemonic in the structured CPU opcode text file.</summary>
        private const int COL_MNEMONIC = 1;
        /// <summary>Column definition for the operand in the structured CPU opcode text file.</summary>
        private const int COL_OPERAND = 2;
        /// <summary>Column definition for the number of bytes in the structured CPU opcode text file.</summary>
        private const int COL_BYTES = 3;
        /// <summary>Column definition for the number of cycles in the structured CPU opcode text file.</summary>
        private const int COL_CYCLES = 4;
        /// <summary>Column definition for the flag changes in the structured CPU opcode text file.</summary>
        private const int COL_FLAGS = 5;
        /// <summary>Column definition for the adress mode in the structured CPU opcode text file.</summary>
        private const int COL_ADDRMODE = 6;
        /// <summary>Column definition for the description in the structured CPU opcode text file.</summary>
        private const int COL_DESCR = 7;
        #endregion Private Constants

        #region Private Fields
        /// <summary>Reference to the currently selected cell in the grid view.</summary>
        private DataGridViewCell currentCell;
        /// <summary>ROM object containing all information of both ROM files.</summary>
        private ROM ROM;

        /// <summary>Stores the last selected section index.</summary>
        private int LastSectionIdx;
        /// <summary>Stores the current section index.</summary>
        private int CurrentSectionIdx;
        /// <summary>Stores the current flag area index.</summary>
        private int CurrentFlagAreaIdx;
        /// <summary>Stores the current instruction index.</summary>
        private int CurrentInstructionIdx;
        /// <summary>Stores the current micro code index.</summary>
        private int CurrentMicroCodeIdx;
        /// <summary>True, if just one instruction is selected.</summary>
        private bool OneInstruction;
        /// <summary>Op code file headers</summary>
        private string[] OpCodeHeaders;
        /// <summary>Op code file columns</summary>
        private string[][] OpCodeCols;
        /// <summary>String definitions for the 3 exception lines.</summary>
        private string[] ExceptionNames = { "RESET", "NMI", "INT" };
        /// <summary>If true, the grid cell and selection changes are suppressed to avoid recursions.</summary>
        private bool suppressChange = false;
        /// <summary>Reference to the open find dialog form. </summary>
        internal frmFind frmFind = null;

        private string ROMdir;
        private string OutDir;
        #endregion Private Fields

        /// <summary>
        /// Creates the main form instance.
        /// </summary>
        public frmMain()
        {
            InitializeComponent();
            ROMdir = Application.StartupPath + ROM_DIR;
            OutDir = Application.StartupPath + OUTPUT_DIR;

            if (Directory.Exists(ROMdir) == false)
                Directory.CreateDirectory(ROMdir);

            if (Directory.Exists(OutDir) == false)
                Directory.CreateDirectory(OutDir);

            openFileDialog1.InitialDirectory=ROMdir;

            ROM = new ROM(ROMdir + ROM_LOW_FILENAME, ROMdir + ROM_HIGH_FILENAME, ROMdir + ROM_TEXT_FILENAME);

            StreamReader sr = new StreamReader(ROMdir + CPU_OPCODE_FILENAME);
            string line = sr.ReadLine();
            OpCodeHeaders = line.Split('\t');
            OpCodeCols = new string[ROM.NO_OF_INSTRUCTIONS][];
            for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                OpCodeCols[i] = sr.ReadLine().Split('\t');
            sr.Close();

            LastSectionIdx = -1;
            cbViewSelection.SelectedIndex = 0;
            foreach (DataGridViewColumn column in dgvCode.Columns)
                column.SortMode = DataGridViewColumnSortMode.NotSortable;

            dgvCode.Columns[0].Frozen = true;
            dgvCode.Columns[1].Frozen = true;

            FillGotoCombobox();
        }

        /// <summary>
        /// Form closing event handler used to save the contents.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAll();
        }


        private void ZipBackup()
        {
            string fname = ("Backup_" + DateTime.Now.ToShortDateString() + "_" + DateTime.Now.ToShortTimeString().Replace(':', '_') + ".zip").Replace('/', '_').Replace(':', '_').Replace(' ','_');
            using (FileStream zipToOpen = new FileStream(ROMdir+fname, FileMode.OpenOrCreate))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    archive.CreateEntryFromFile(ROMdir + ROM_TEXT_FILENAME, ROM_TEXT_FILENAME);
                    archive.CreateEntryFromFile(ROMdir + ROM_HIGH_FILENAME, ROM_HIGH_FILENAME);
                    archive.CreateEntryFromFile(ROMdir + ROM_HIGH_FILENAME, ROM_HIGH_FILENAME);
                }
            }
        }

        private void loadFromBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (ZipArchive archive = ZipFile.OpenRead(openFileDialog1.FileName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                        entry.ExtractToFile(ROMdir+entry.Name, true);
                }
                ROM = new ROM(ROMdir + ROM_LOW_FILENAME, ROMdir + ROM_HIGH_FILENAME, ROMdir + ROM_TEXT_FILENAME);
                InitGrid();
            }
        }

        /// <summary>
        /// Saves the current contents of all micro codes to the three files plus the generated historgram. If the target folder is valid, the ROM files are copied there too. 
        /// This could be the schematics folder, where DigiSim expects the ROM files.
        /// </summary>
        private void SaveAll()
        {
            ZipBackup();

            ROM.SaveToFiles();
            SaveHistograms();

            if (Directory.Exists(tbROMtargetFolder.Text))
            {
                try
                {
                    File.Copy(ROMdir + ROM_LOW_FILENAME, tbROMtargetFolder.Text + "\\" + ROM_LOW_FILENAME, true);
                    File.Copy(ROMdir + ROM_HIGH_FILENAME, tbROMtargetFolder.Text + "\\" + ROM_HIGH_FILENAME, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        /// <summary>
        /// Create and save th historgram of the used bit fields. This can help eleminating unused codes.
        /// </summary>
        public void SaveHistograms()
        {
            List<string> HDCY = new List<string>();
            List<string> FDCY = new List<string>();

            int[] histAddrOut = new int[1 << MicroCode.ADDR_ENABLE_BITS];
            int[] histOutputEn = new int[1 << MicroCode.OUTPUT_ENABLE_BITS];
            int[] histLoadSel = new int[1 << MicroCode.LOAD_SEL_BITS];
            int[] histAluCode = new int[1 << MicroCode.ALU_CODE_BITS];

            for (int i = 0; i < histAddrOut.Length; i++)
                histAddrOut[i] = 0;

            for (int i = 0; i < histOutputEn.Length; i++)
                histOutputEn[i] = 0;

            for (int i = 0; i < histLoadSel.Length; i++)
                histLoadSel[i] = 0;

            for (int i = 0; i < histAluCode.Length; i++)
                histAluCode[i] = 0;

            for (int s = 0; s < ROM.NO_OF_GLOBAL_SECTIONS; s++)
                for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                    for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                        for (int m = 0; m < ROM.MICRO_CODES_PER_INSTRUCTION; m++)
                        {
                            histAddrOut[ROM.MicroCodes[s][f][i][m].AddrOut]++;
                            histOutputEn[ROM.MicroCodes[s][f][i][m].OutputEn]++;
                            histLoadSel[ROM.MicroCodes[s][f][i][m].LoadSel]++;
                            histAluCode[ROM.MicroCodes[s][f][i][m].AluCode]++;

                            if (ROM.MicroCodes[s][f][i][m].OutputEn == 29)
                                HDCY.Add(cbGotoCode.Items[i].ToString().Replace(',', '.') + ",Section=" + s.ToString() + ",Flag=" + f.ToString() + ",MC=" + m.ToString());
                            if (ROM.MicroCodes[s][f][i][m].OutputEn == 30)
                                FDCY.Add(cbGotoCode.Items[i].ToString().Replace(',', '.') + ",Section=" + s.ToString() + ",Flag=" + f.ToString() + ",MC=" + m.ToString());
                        }
            try
            {
                StreamWriter sw = new StreamWriter(OutDir + ROM_HIST_FILENAME);
                sw.Write("Group");
                int nmax = Math.Max(histAddrOut.Length, Math.Max(histOutputEn.Length, Math.Max(histLoadSel.Length, histAluCode.Length)));
                for (int i = 0; i < nmax; i++)
                    sw.Write("," + i.ToString());
                sw.WriteLine("\n");

                sw.Write("AddrOut");
                for (int i = 0; i < cbAdressEnable.Items.Count; i++)
                    sw.Write("," + cbAdressEnable.Items[i]);
                sw.WriteLine("");
                sw.Write("AddrOut");
                for (int i = 0; i < histAddrOut.Length; i++)
                    sw.Write("," + histAddrOut[i].ToString());
                sw.WriteLine("\n");

                sw.Write("OutputEn");
                for (int i = 0; i < cbOutEnable.Items.Count; i++)
                    sw.Write("," + cbOutEnable.Items[i]);
                sw.WriteLine("");
                sw.Write("OutputEn");
                for (int i = 0; i < histOutputEn.Length; i++)
                    sw.Write("," + histOutputEn[i].ToString());
                sw.WriteLine("\n");

                sw.Write("LoadSel");
                for (int i = 0; i < cbLoadCode.Items.Count; i++)
                    sw.Write("," + cbLoadCode.Items[i]);
                sw.WriteLine("");
                sw.Write("LoadSel");
                for (int i = 0; i < histLoadSel.Length; i++)
                    sw.Write("," + histLoadSel[i].ToString());
                sw.WriteLine("\n");

                sw.Write("AluCode");
                for (int i = 0; i < cbAluCode.Items.Count; i++)
                    sw.Write("," + cbAluCode.Items[i]);
                sw.WriteLine("");
                sw.Write("AluCode");
                for (int i = 0; i < histAluCode.Length; i++)
                    sw.Write("," + histAluCode[i].ToString());
                sw.WriteLine("");
                sw.WriteLine("");

                sw.WriteLine("FDCY");
                for (int i = 0; i < FDCY.Count; i++)
                    sw.WriteLine(FDCY[i]);
                sw.WriteLine("");

                sw.WriteLine("HDCY");
                for (int i = 0; i < HDCY.Count; i++)
                    sw.WriteLine(HDCY[i]);

                sw.Close();
            }
            catch { }
        }

        private void FillGotoCombobox()
        {
            for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
            {
                string[] col = OpCodeCols[i];

                string opcode = "0x" + col[COL_OPCODE];
                string mnemonic = col[COL_MNEMONIC];
                string descr = col[COL_DESCR];
                string s = opcode + ": ";

                if (mnemonic != "")
                    s += mnemonic.PadRight(16) + descr;

                cbGotoCode.Items.Add(s);
            }
        }



        /// <summary>
        /// Initialize the grid view for the opcode section. 
        /// </summary>
        private void InitOpCodeGrid()
        {
            if ((dtCodeView.Rows == null) || (dtCodeView.Rows.Count != ROM.NO_OF_INSTRUCTIONS * LINES_PER_INSTRUCTION))
            {
                dtCodeView.Clear();
                for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                    for (int j = 0; j < LINES_PER_INSTRUCTION; j++)
                    {
                        DataRow row = dtCodeView.NewRow();
                        dtCodeView.Rows.Add(row);
                    }
            }
            cbGotoCode.Enabled = true;

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(OutDir + GRID_CSV_FILENAME + CurrentSectionIdx.ToString() + "_Flag_" + CurrentFlagAreaIdx.ToString() + ".prn");
                foreach (DataColumn col in dtCodeView.Columns)
                    sw.Write(col.ColumnName + "\t");
                sw.WriteLine();
            }
            catch { }

            int rowIdx = 0;
            for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
            {
                string[] col = OpCodeCols[i];

                string opcode = "0x" + col[COL_OPCODE];
                string mnemonic = col[COL_MNEMONIC];
                string descr = col[COL_DESCR];
                string addrmode = col[COL_ADDRMODE];
                string operand = col[COL_OPERAND];
                string bytes = col[COL_BYTES];
                string cycles = col[COL_CYCLES];
                string flags = col[COL_FLAGS];
                string s = opcode + ": ";
                string soperand = operand;
                string saddrmode = addrmode;
                string sbytes = bytes;
                string scycles = cycles;
                string sflags = flags;

                if (mnemonic != "")
                {
                    s += mnemonic.PadRight(16) + descr;
                    soperand = OpCodeHeaders[COL_OPERAND] + ": " + operand;
                    saddrmode = OpCodeHeaders[COL_ADDRMODE] + ": " + addrmode;
                    sbytes = OpCodeHeaders[COL_BYTES] + ": " + bytes;
                    scycles = OpCodeHeaders[COL_CYCLES] + ": " + cycles;
                    sflags = OpCodeHeaders[COL_FLAGS] + ": " + col[COL_FLAGS];
                }

                if (CurrentSectionIdx != LastSectionIdx)
                {
                    cbCopyFrom.Items.Add(s);
                    cbCopyTo.Items.Add(s);
                }

                for (int j = 0; j < LINES_PER_INSTRUCTION; j++)
                {
                    DataRow row = dtCodeView.Rows[rowIdx++];
                    for (int k = 0; k < ROM.MICRO_CODES_PER_INSTRUCTION; k++)
                    {
                        switch (j)
                        {
                            case 0:
                                row[0] = opcode;
                                row[1] = mnemonic;
                                row[k + 2] = ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Comment;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].OpCode = opcode;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Mnemonic = mnemonic;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Description = descr;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].AddrMode = addrmode;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Bytes = bytes;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Cycles = cycles;
                                break;

                            case 1:
                                row[k + 2] = cbAdressEnable.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].AddrOut].ToString();
                                row[1] = descr;
                                break;

                            case 2:
                                row[k + 2] = cbOutEnable.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].OutputEn].ToString();
                                row[1] = saddrmode;
                                break;

                            case 3:
                                row[k + 2] = cbLoadCode.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].LoadSel].ToString();
                                row[1] = sbytes + "  " + scycles + "  " + soperand;
                                break;

                            case 4:
                                row[k + 2] = cbAluCode.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].AluCode].ToString();
                                row[1] = sflags;
                                break;
                        }
                    }
                    try
                    {
                        for (int c = 0; c < row.ItemArray.Length; c++)
                            sw.Write(row[c] + "\t");
                        sw.WriteLine();
                    }
                    catch { }
                }
            }
            try { sw.Close(); } catch { }
        }

        /// <summary>
        /// Gets a limited version of the cbGotoCode.SelectedIndex in the valid range to avoid out-of-range exceptions.
        /// </summary>
        private int CodeIdx
        {
            get { return Math.Min(Math.Max(cbGotoCode.SelectedIndex, 0), cbGotoCode.Items.Count - 1); }
        }

        /// <summary>
        /// Initialize the grid view for the one instruction. 
        /// </summary>
        private void InitOneInstructionGrid()
        {
            if ((dtCodeView.Rows == null) || (dtCodeView.Rows.Count != ROM.FLAG_AREA_COUNT * LINES_PER_INSTRUCTION))
            {
                dtCodeView.Clear();
                int rIdx = 0;
                for (int i = 0; i < ROM.FLAG_AREA_COUNT; i++)
                    for (int j = 0; j < LINES_PER_INSTRUCTION; j++, rIdx++)
                    {
                        DataRow row = dtCodeView.NewRow();
                        dtCodeView.Rows.Add(row);

                        Color color = Color.White;
                        if ((i == 0) || (i == ROM.FLAG_AREA_COUNT / 2))
                            color = Color.FromArgb(0xF0, 0xF0, 0xF0);

                        for (int k = 0; k < dgvCode.Columns.Count; k++)
                            dgvCode.Rows[rIdx].Cells[k].Style.BackColor = color;
                    }
            }

            cbGotoCode.Enabled = true;

            int ocidx = CodeIdx;
            string[] col = OpCodeCols[ocidx];

            string opcode = "0x" + col[COL_OPCODE];
            string mnemonic = col[COL_MNEMONIC];
            string descr = col[COL_DESCR];
            string addrmode = col[COL_ADDRMODE];
            string operand = col[COL_OPERAND];
            string bytes = col[COL_BYTES];
            string cycles = col[COL_CYCLES];
            string flags = col[COL_FLAGS];
            string s = opcode + ": ";
            string soperand = operand;
            string saddrmode = addrmode;
            string sbytes = bytes;
            string scycles = cycles;
            string sflags = flags;

            if (mnemonic != "")
            {
                s += mnemonic.PadRight(16) + descr;
                soperand = OpCodeHeaders[COL_OPERAND] + ": " + operand;
                saddrmode = OpCodeHeaders[COL_ADDRMODE] + ": " + addrmode;
                sbytes = OpCodeHeaders[COL_BYTES] + ": " + bytes;
                scycles = OpCodeHeaders[COL_CYCLES] + ": " + cycles;
                sflags = OpCodeHeaders[COL_FLAGS] + ": " + col[COL_FLAGS];
            }

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(OutDir + GRID_CSV_FILENAME + opcode + "_" + mnemonic.Replace(' ', '_') + ".prn");
                foreach (DataColumn dc in dtCodeView.Columns)
                    sw.Write(dc.ColumnName + "\t");
                sw.WriteLine();
            }
            catch { }

            int rowIdx = 0;
            for (int CurrentFlagAreaIdx = 0; CurrentFlagAreaIdx < ROM.FLAG_AREA_COUNT; CurrentFlagAreaIdx++)
            {
                string[] fstr = new string[] { "FC:" + (CurrentFlagAreaIdx & 1).ToString(), "HC:" + ((CurrentFlagAreaIdx >> 1) & 1).ToString(), "SC:" + ((CurrentFlagAreaIdx >> 2) & 1).ToString() };

                if (CurrentSectionIdx != LastSectionIdx)
                {
                    string ss = CurrentFlagAreaIdx.ToString() + ". " + fstr[2] + ", " + fstr[1] + ", " + fstr[0] + ", " + ": " + s;
                    cbCopyFrom.Items.Add(ss);
                    cbCopyTo.Items.Add(ss);
                }

                for (int j = 0; j < LINES_PER_INSTRUCTION; j++)
                {
                    DataRow row = dtCodeView.Rows[rowIdx++];
                    for (int k = 0; k < ROM.MICRO_CODES_PER_INSTRUCTION; k++)
                    {
                        switch (j)
                        {
                            case 0:
                                row[0] = opcode;
                                row[1] = mnemonic;
                                row[k + 2] = ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].Comment;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].OpCode = opcode;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].Mnemonic = mnemonic;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].Description = descr;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].AddrMode = addrmode;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].Bytes = bytes;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].Cycles = cycles;
                                break;

                            case 1:
                                row[0] = fstr[0];
                                row[k + 2] = cbAdressEnable.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].AddrOut].ToString();
                                row[1] = descr;
                                break;

                            case 2:
                                row[0] = fstr[1];
                                row[k + 2] = cbOutEnable.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].OutputEn].ToString();
                                row[1] = saddrmode;
                                break;

                            case 3:
                                row[0] = fstr[2];
                                row[k + 2] = cbLoadCode.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].LoadSel].ToString();
                                row[1] = sbytes + "  " + scycles + "  " + soperand;
                                break;

                            case 4:
                                row[k + 2] = cbAluCode.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][ocidx][k].AluCode].ToString();
                                row[1] = sflags;
                                break;
                        }
                    }
                    try
                    {
                        for (int c = 0; c < row.ItemArray.Length; c++)
                            sw.Write(row[c] + "\t");
                        sw.WriteLine();
                    }
                    catch { }
                }
            }
            try { sw.Close(); } catch { }
        }


        /// <summary>
        /// Returns the code of the highest priority excception. If Reset is low, the other 2 don't matter. If NMI is low, INT doesn't matter.
        /// </summary>
        /// <param name="Code">The code containing the 3 exception bit as lsbs</param>
        /// <returns>Number of the highest priority exception</returns>
        private int GetHighestException(int Code)
        {
            // Return enumerator of the highest priority Exception
            if (((Code & 1) == 0) || (Code & 7) == 7)
                return 0;
            else if ((Code & 2) == 0)
                return 1;
            else
                return 2;
        }

        /// <summary>
        /// Initialize the grid view for the Exception section.
        /// </summary>
        private void InitExceptionGrid()
        {
            if ((dtCodeView.Rows == null) || (dtCodeView.Rows.Count != NO_OF_EXCEPTION_FIELDS * LINES_PER_INSTRUCTION))
            {
                dtCodeView.Clear();
                int rIdx = 0;
                for (int i = 0; i < NO_OF_EXCEPTION_FIELDS; i++)
                    for (int j = 0; j < LINES_PER_INSTRUCTION; j++, rIdx++)
                    {
                        DataRow row = dtCodeView.NewRow();
                        dtCodeView.Rows.Add(row);

                        Color color = Color.White;
                        if (GetHighestException(i) == 0)
                            color = Color.FromArgb(0xF0, 0xF0, 0xF0);

                        for (int k = 0; k < dgvCode.Columns.Count; k++)
                            dgvCode.Rows[rIdx].Cells[k].Style.BackColor = color;
                    }
            }

            cbGotoCode.Enabled = false;
            if (CurrentSectionIdx != LastSectionIdx)
                for (int i = 0; i < ExceptionNames.Length; i++)
                {
                    cbCopyFrom.Items.Add(ExceptionNames[i]);
                    cbCopyTo.Items.Add(ExceptionNames[i]);
                }

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(OutDir + GRID_CSV_FILENAME + CurrentSectionIdx.ToString() + "_Flag_" + CurrentFlagAreaIdx.ToString() + ".prn");
                foreach (DataColumn col in dtCodeView.Columns)
                    sw.Write(col.ColumnName + "\t");
                sw.WriteLine();
            }
            catch { }

            int rowIdx = 0;
            for (int i = 0; i < NO_OF_EXCEPTION_FIELDS; i++)
            {
                string exccode = "0x" + i.ToString("X");
                int exctype = GetHighestException(i);
                string mnemonic = ExceptionNames[exctype];
                string descr = "";

                for (int j = 0; j < LINES_PER_INSTRUCTION; j++)
                {
                    DataRow row = dtCodeView.Rows[rowIdx++];
                    for (int k = 0; k < ROM.MICRO_CODES_PER_INSTRUCTION; k++)
                    {
                        switch (j)
                        {
                            case 0:
                                row[0] = exccode;
                                row[1] = mnemonic;
                                row[k + 2] = ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Comment;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].OpCode = exccode;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Mnemonic = mnemonic;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Description = descr;
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].AddrMode = "";
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Bytes = "";
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].Cycles = "";
                                break;

                            case 1:
                                row[k + 2] = cbAdressEnable.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].AddrOut].ToString();
                                row[1] = descr;
                                break;

                            case 2:
                                row[k + 2] = cbOutEnable.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].OutputEn].ToString();
                                //row[1] = addrmode;
                                break;

                            case 3:
                                row[k + 2] = cbLoadCode.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].LoadSel].ToString();
                                //row[1] = bytescycles;
                                break;

                            case 4:
                                row[k + 2] = cbAluCode.Items[ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][k].AluCode].ToString();
                                break;

                        }
                    }
                    try
                    {
                        for (int c = 0; c < row.ItemArray.Length; c++)
                            sw.Write(row[c] + "\t");
                        sw.WriteLine();
                    }
                    catch { }
                }
            }
            try { sw.Close(); } catch { }
        }

        /// <summary>
        /// Initialize the grid view after any selection change.
        /// </summary>
        private void InitGrid()
        {
            if (CurrentSectionIdx != LastSectionIdx)
            {
                cbCopyFrom.SelectedItem = null;
                cbCopyTo.SelectedItem = null;
                cbCopyFrom.Items.Clear();
                cbCopyTo.Items.Clear();
            }
            dgvCode.ClearSelection();

            if (OneInstruction)
                InitOneInstructionGrid();
            else if (CurrentSectionIdx == 0)
                InitOpCodeGrid();
            else
                InitExceptionGrid();

            LastSectionIdx = CurrentSectionIdx;
        }

        /// <summary>
        /// Grid view cell click event handler to overlay or disappear the combo boxes of the different opcode fields.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void dgvCode_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            suppressChange = true;
            currentCell = dgvCode.CurrentCell;
            if (OneInstruction)
            {
                CurrentInstructionIdx = CodeIdx;
                CurrentFlagAreaIdx = e.RowIndex / LINES_PER_INSTRUCTION;
            }
            else
            {
                CurrentInstructionIdx = e.RowIndex / LINES_PER_INSTRUCTION;
            }
            CurrentMicroCodeIdx = e.ColumnIndex - 2;

            if (e.ColumnIndex >= 2)
            {
                Rectangle rect = dgvCode.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                switch (e.RowIndex % LINES_PER_INSTRUCTION)
                {
                    case 0:
                        //ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][CurrentInstructionIdx][CurrentMicroCodeIdx].Comment = dgvCode.CurrentCell.Value.ToString();
                        cbAdressEnable.Visible = false;
                        cbOutEnable.Visible = false;
                        cbLoadCode.Visible = false;
                        cbAluCode.Visible = false;
                        break;

                    case 1:
                        cbAdressEnable.SetBounds(dgvCode.Location.X + rect.X, dgvCode.Location.Y + rect.Y, rect.Width, rect.Height);
                        cbAdressEnable.SelectedIndex = cbAdressEnable.Items.IndexOf(dgvCode.CurrentCell.Value.ToString());
                        cbAdressEnable.Visible = true;
                        cbOutEnable.Visible = false;
                        cbLoadCode.Visible = false;
                        cbAluCode.Visible = false;
                        break;

                    case 2:
                        cbOutEnable.SetBounds(dgvCode.Location.X + rect.X, dgvCode.Location.Y + rect.Y, rect.Width, rect.Height);
                        cbOutEnable.SelectedIndex = cbOutEnable.Items.IndexOf(dgvCode.CurrentCell.Value.ToString());
                        cbAdressEnable.Visible = false;
                        cbOutEnable.Visible = true;
                        cbLoadCode.Visible = false;
                        cbAluCode.Visible = false;
                        break;

                    case 3:
                        cbLoadCode.SetBounds(dgvCode.Location.X + rect.X, dgvCode.Location.Y + rect.Y, rect.Width, rect.Height);
                        cbLoadCode.SelectedIndex = cbLoadCode.Items.IndexOf(dgvCode.CurrentCell.Value.ToString());
                        cbAdressEnable.Visible = false;
                        cbOutEnable.Visible = false;
                        cbLoadCode.Visible = true;
                        cbAluCode.Visible = false;
                        break;

                    case 4:
                        cbAluCode.SetBounds(dgvCode.Location.X + rect.X, dgvCode.Location.Y + rect.Y, rect.Width, rect.Height);
                        cbAluCode.SelectedIndex = cbAluCode.Items.IndexOf(dgvCode.CurrentCell.Value.ToString());
                        cbAdressEnable.Visible = false;
                        cbOutEnable.Visible = false;
                        cbLoadCode.Visible = false;
                        cbAluCode.Visible = true;
                        break;

                }
            }
            else
            {
                cbAdressEnable.Visible = false;
                cbOutEnable.Visible = false;
                cbLoadCode.Visible = false;
                cbAluCode.Visible = false;
            }
            suppressChange = false;
        }

        /// <summary>
        /// Grid view cell end edit event handler to capture the description text and copy it to the micro code comment.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void dgvCode_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if ((e.ColumnIndex >= 2) && ((e.RowIndex % LINES_PER_INSTRUCTION) == 0))
            {
                if (OneInstruction)
                {
                    CurrentInstructionIdx = CodeIdx;
                    CurrentFlagAreaIdx = e.RowIndex / LINES_PER_INSTRUCTION;
                }
                else
                {
                    CurrentInstructionIdx = e.RowIndex / LINES_PER_INSTRUCTION;
                }
                CurrentMicroCodeIdx = e.ColumnIndex - 2;

                string s = dgvCode.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                if (CurrentSectionIdx == 0)
                {
                    if ((CurrentFlagAreaIdx == 0) && (ckbAutoCopyBaseViewChangesToFlagAreas.Checked == true))
                        for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                            ROM.MicroCodes[CurrentSectionIdx][f][CurrentInstructionIdx][CurrentMicroCodeIdx].Comment = s;
                    else
                        ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][CurrentInstructionIdx][CurrentMicroCodeIdx].Comment = s;

                    if (OneInstruction && (CurrentFlagAreaIdx == 0) && ckbAutoCopyBaseViewChangesToFlagAreas.Checked)
                        InitGrid();
                }
                else
                {
                    int type = GetHighestException(CurrentInstructionIdx);

                    for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                        for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                        {
                            if (GetHighestException(i) == type)
                                ROM.MicroCodes[CurrentSectionIdx][f][i][CurrentMicroCodeIdx].Comment = s;
                        }
                    InitGrid();
                }
            }
        }

        /// <summary>
        /// Grid view scroll event handler to disapear all overlaid combo boxes.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void dgvCode_Scroll(object sender, ScrollEventArgs e)
        {
            cbAdressEnable.Visible = false;
            cbOutEnable.Visible = false;
            cbLoadCode.Visible = false;
            cbAluCode.Visible = false;
        }

        /// <summary>
        /// Combo box event handler for changing the adress enable bit selection.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void cbAdressEnable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressChange == true) return;
            currentCell.Value = cbAdressEnable.Text;
            if (CurrentSectionIdx == 0)
            {
                if ((CurrentFlagAreaIdx == 0) && (ckbAutoCopyBaseViewChangesToFlagAreas.Checked == true))
                    for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                        ROM.MicroCodes[CurrentSectionIdx][f][CurrentInstructionIdx][CurrentMicroCodeIdx].AddrOut = cbAdressEnable.SelectedIndex;
                else
                    ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][CurrentInstructionIdx][CurrentMicroCodeIdx].AddrOut = cbAdressEnable.SelectedIndex;

                if (OneInstruction && (CurrentFlagAreaIdx == 0) && ckbAutoCopyBaseViewChangesToFlagAreas.Checked)
                    InitGrid();
            }
            else
            {
                int type = GetHighestException(CurrentInstructionIdx);

                for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                    for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                    {
                        if (GetHighestException(i) == type)
                            ROM.MicroCodes[CurrentSectionIdx][f][i][CurrentMicroCodeIdx].AddrOut = cbAdressEnable.SelectedIndex;
                    }
                InitGrid();
            }
            cbAdressEnable.Visible = false;
        }

        /// <summary>
        /// Combo box event handler for changing the output enable bit selection.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void cbOutEnable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressChange == true) return;
            currentCell.Value = cbOutEnable.Text;
            if (CurrentSectionIdx == 0)
            {
                if ((CurrentFlagAreaIdx == 0) && (ckbAutoCopyBaseViewChangesToFlagAreas.Checked == true))
                    for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                        ROM.MicroCodes[CurrentSectionIdx][f][CurrentInstructionIdx][CurrentMicroCodeIdx].OutputEn = cbOutEnable.SelectedIndex;
                else
                    ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][CurrentInstructionIdx][CurrentMicroCodeIdx].OutputEn = cbOutEnable.SelectedIndex;

                if (OneInstruction && (CurrentFlagAreaIdx == 0) && ckbAutoCopyBaseViewChangesToFlagAreas.Checked)
                    InitGrid();
            }
            else
            {
                int type = GetHighestException(CurrentInstructionIdx);

                for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                    for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                    {
                        if (GetHighestException(i) == type)
                            ROM.MicroCodes[CurrentSectionIdx][f][i][CurrentMicroCodeIdx].OutputEn = cbOutEnable.SelectedIndex;
                    }
                InitGrid();
            }
            cbOutEnable.Visible = false;
        }

        /// <summary>
        /// Combo box event handler for changing the load code bit selection.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void cbLoadCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressChange == true) return;
            currentCell.Value = cbLoadCode.Text;
            if (CurrentSectionIdx == 0)
            {
                if ((CurrentFlagAreaIdx == 0) && (ckbAutoCopyBaseViewChangesToFlagAreas.Checked == true))
                    for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                        ROM.MicroCodes[CurrentSectionIdx][f][CurrentInstructionIdx][CurrentMicroCodeIdx].LoadSel = cbLoadCode.SelectedIndex;
                else
                    ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][CurrentInstructionIdx][CurrentMicroCodeIdx].LoadSel = cbLoadCode.SelectedIndex;

                if (OneInstruction && (CurrentFlagAreaIdx == 0) && ckbAutoCopyBaseViewChangesToFlagAreas.Checked)
                    InitGrid();
            }
            else
            {
                int type = GetHighestException(CurrentInstructionIdx);

                for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                    for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                    {
                        if (GetHighestException(i) == type)
                            ROM.MicroCodes[CurrentSectionIdx][f][i][CurrentMicroCodeIdx].LoadSel = cbLoadCode.SelectedIndex;
                    }
                InitGrid();
            }
            cbLoadCode.Visible = false;
        }

        /// <summary>
        /// Combo box event handler for changing the ALU code bit selection.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void cbAluCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressChange == true) return;
            currentCell.Value = cbAluCode.Text;
            if (CurrentSectionIdx == 0)
            {
                if ((CurrentFlagAreaIdx == 0) && (ckbAutoCopyBaseViewChangesToFlagAreas.Checked == true))
                    for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                        ROM.MicroCodes[CurrentSectionIdx][f][CurrentInstructionIdx][CurrentMicroCodeIdx].AluCode = cbAluCode.SelectedIndex;
                else
                    ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][CurrentInstructionIdx][CurrentMicroCodeIdx].AluCode = cbAluCode.SelectedIndex;

                if (OneInstruction && (CurrentFlagAreaIdx == 0) && ckbAutoCopyBaseViewChangesToFlagAreas.Checked)
                    InitGrid();
            }
            else
            {
                int type = GetHighestException(CurrentInstructionIdx);

                for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                    for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                    {
                        if (GetHighestException(i) == type)
                            ROM.MicroCodes[CurrentSectionIdx][f][i][CurrentMicroCodeIdx].AluCode = cbAluCode.SelectedIndex;
                    }
                InitGrid();
            }
            cbAluCode.Visible = false;
        }

        /// <summary>
        /// Combo box event handler for changing the view selection.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void cbViewSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbViewSelection.SelectedIndex <= 8)
            {
                CurrentSectionIdx = cbViewSelection.SelectedIndex >> 3;
                CurrentFlagAreaIdx = cbViewSelection.SelectedIndex & 0x7;
                if (OneInstruction)
                {
                    LastSectionIdx = -1;
                    dtCodeView.Clear();
                }
                OneInstruction = false;
            }
            else
            {
                CurrentSectionIdx = 0;
                CurrentFlagAreaIdx = 0;
                if (OneInstruction == false)
                    dtCodeView.Clear();
                OneInstruction = true;
                LastSectionIdx = -1;
            }

            //int idx = CodeIdx;
            InitGrid();

            //if (idx >= 0)
            //    try { cbGotoCode.SelectedIndex = idx; }
            //    catch { }
        }

        /// <summary>
        /// Button click event handler to copy the entries from one instruction to another.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void btnCopyEntries_Click(object sender, EventArgs e)
        {
            try
            {
                if (OneInstruction)
                {
                    for (int m = 0; m < ROM.MICRO_CODES_PER_INSTRUCTION; m++)
                        ROM.MicroCodes[CurrentSectionIdx][cbCopyFrom.SelectedIndex][CodeIdx][m].CopyTo(ROM.MicroCodes[CurrentSectionIdx][cbCopyTo.SelectedIndex][CodeIdx][m]);
                    InitGrid();
                }
                else
                {
                    if (CurrentSectionIdx == 0)
                    {
                        if (ckbCopyAllSourceFlagAreasToTargetFlagAreas.Checked == true)
                        {
                            for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                                for (int m = 0; m < ROM.MICRO_CODES_PER_INSTRUCTION; m++)
                                    ROM.MicroCodes[CurrentSectionIdx][f][cbCopyFrom.SelectedIndex][m].CopyTo(ROM.MicroCodes[CurrentSectionIdx][f][cbCopyTo.SelectedIndex][m]);
                        }
                        else
                        {
                            for (int m = 0; m < ROM.MICRO_CODES_PER_INSTRUCTION; m++)
                                ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][cbCopyFrom.SelectedIndex][m].CopyTo(ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][cbCopyTo.SelectedIndex][m]);
                        }
                    }
                    else
                    {
                        int code = -1;
                        for (int i = 0; i < NO_OF_EXCEPTION_FIELDS; i++)
                            if (ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][0].Mnemonic == ExceptionNames[cbCopyTo.SelectedIndex])
                            {
                                code = Convert.ToInt32(ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][i][0].OpCode.Substring(2));
                                break;
                            }

                        int type = GetHighestException(code);

                        for (int f = 0; f < ROM.FLAG_AREA_COUNT; f++)
                            for (int i = 0; i < ROM.NO_OF_INSTRUCTIONS; i++)
                            {
                                if (GetHighestException(i) == type)
                                    for (int m = 0; m < ROM.MICRO_CODES_PER_INSTRUCTION; m++)
                                        ROM.MicroCodes[CurrentSectionIdx][CurrentFlagAreaIdx][cbCopyFrom.SelectedIndex][m].CopyTo(ROM.MicroCodes[CurrentSectionIdx][f][i][m]);
                            }
                    }
                    InitGrid();
                    cbGotoCode.SelectedIndex = cbCopyTo.SelectedIndex;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        /// <summary>
        /// Menu item click event handler to save the contents.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAll();
        }

        /// <summary>
        /// Button click event handler to copy the entries from one instruction to all following.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void btnCopyToAllFollowing_Click(object sender, EventArgs e)
        {
            while (cbCopyTo.SelectedIndex < cbCopyTo.Items.Count - 1)
            {
                btnCopyEntries_Click(sender, e);
                cbCopyTo.SelectedIndex = cbCopyTo.SelectedIndex + 1;
            }
        }

        /// <summary>
        /// Combo box event handler for changing the Goto code selection.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void cbGotoCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((cbGotoCode.SelectedIndex >= 0) && (cbGotoCode.SelectedIndex < cbGotoCode.Items.Count))
            {
                if (OneInstruction)
                {
                    LastSectionIdx = -1;
                    InitGrid();
                }
                else
                {
                    dgvCode.FirstDisplayedScrollingRowIndex = cbGotoCode.SelectedIndex * LINES_PER_INSTRUCTION;
                }
            }
        }

        /// <summary>
        /// Menu item click event handler to open the find form for navigating to a specific code.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void tsmiFind_Click(object sender, EventArgs e)
        {
            if (frmFind == null)
                frmFind = new frmFind(this);

            if (frmFind.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < cbGotoCode.Items.Count; i++)
                    if (cbGotoCode.Items[i].ToString().Contains(frmFind.FindText))
                    {
                        cbGotoCode.SelectedIndex = i; break;
                    }

            }
        }

        private DataGridViewCell[] selectedCells;
        private bool cutMode = false;

        private void dgvCode_MouseClick(object sender, MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Right) && (dgvCode.SelectedCells.Count > 0))
            {
                tsmiPaste.Enabled = (selectedCells != null) && (selectedCells.Length > 0) && (currentCell != null) && (currentCell.ColumnIndex >= 2);
                cmsCellActions.Show(dgvCode, e.Location);
            }
        }

        private void tsmiCut_Click(object sender, EventArgs e)
        {
            selectedCells = new DataGridViewCell[dgvCode.SelectedCells.Count];
            dgvCode.SelectedCells.CopyTo(selectedCells, 0);
            cutMode = true;
        }

        private void tsmiCopy_Click(object sender, EventArgs e)
        {
            selectedCells = new DataGridViewCell[dgvCode.SelectedCells.Count];
            dgvCode.SelectedCells.CopyTo(selectedCells, 0);
            cutMode = false;
        }

        private void tsmiClear_Click(object sender, EventArgs e)
        {
            selectedCells = new DataGridViewCell[dgvCode.SelectedCells.Count];
            dgvCode.SelectedCells.CopyTo(selectedCells, 0);
            ClearSelected();
        }

        private int MatchAddressEnable(string Text)
        {
            for (int i = 0; i < cbAdressEnable.Items.Count; i++)
                if (cbAdressEnable.Items[i].ToString().Trim() == Text.Trim())
                    return i;
            return -1;
        }

        private int MatchOutEnable(string Text)
        {
            for (int i = 0; i < cbOutEnable.Items.Count; i++)
                if (cbOutEnable.Items[i].ToString().Trim() == Text.Trim())
                    return i;
            return -1;
        }

        private int MatchLoadCode(string Text)
        {
            for (int i = 0; i < cbLoadCode.Items.Count; i++)
                if (cbLoadCode.Items[i].ToString().Trim() == Text.Trim())
                    return i;
            return -1;
        }

        private int MatchAluCode(string Text)
        {
            for (int i = 0; i < cbAluCode.Items.Count; i++)
                if (cbAluCode.Items[i].ToString().Trim() == Text.Trim())
                    return i;
            return -1;
        }

        private void PasteSelected(int ColMin, int RowMin, bool Write)
        {
            foreach (DataGridViewCell cell in selectedCells)
            {
                int tgtCol = currentCell.ColumnIndex + (cell.ColumnIndex - ColMin);
                int tgtRow = currentCell.RowIndex + (cell.RowIndex - RowMin);
                DataGridViewCell tgtCell = dgvCode.Rows[tgtRow].Cells[tgtCol]; 

                int currentSectionIdx = CurrentSectionIdx;
                int currentFlagAreaIdx = CurrentFlagAreaIdx;
                int currentInstructionIdx = CurrentInstructionIdx;
                int currentMicroCodeIdx = CurrentMicroCodeIdx;

                if (OneInstruction)
                {
                    currentInstructionIdx = CodeIdx;
                    currentFlagAreaIdx = tgtCell.RowIndex / LINES_PER_INSTRUCTION;
                }
                else
                {
                    currentInstructionIdx = tgtCell.RowIndex / LINES_PER_INSTRUCTION;
                }
                currentMicroCodeIdx = tgtCell.ColumnIndex - 2;

                int idx = 0;
                switch (tgtCell.RowIndex % LINES_PER_INSTRUCTION)
                {
                    case 0:
                        if (Write)
                        {
                            ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].Comment = cell.Value.ToString();
                        }
                        break;

                    case 1:
                        idx = MatchAddressEnable(cell.Value.ToString());
                        if (Write && idx >= 0)
                            ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].AddrOut = idx;
                        break;

                    case 2:
                        idx = MatchOutEnable(cell.Value.ToString());
                        if (Write && idx >= 0)
                            ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].OutputEn = idx;
                        break;

                    case 3:
                        idx = MatchLoadCode(cell.Value.ToString());
                        if (Write && idx >= 0)
                            ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].LoadSel = idx;
                        break;

                    case 4:
                        idx = MatchAluCode(cell.Value.ToString());
                        if (Write && idx >= 0)
                            ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].AluCode = idx;
                        break;
                }
                if (idx < 0)
                    throw new Exception();

                if (Write)
                    tgtCell.Value = cell.Value;
            }
        }

        private void ClearSelected()
        {
            foreach (DataGridViewCell cell in selectedCells)
            {
                DataGridViewCell tgtCell = dgvCode.Rows[cell.RowIndex].Cells[cell.ColumnIndex];

                int currentSectionIdx = CurrentSectionIdx;
                int currentFlagAreaIdx = CurrentFlagAreaIdx;
                int currentInstructionIdx = CurrentInstructionIdx;
                int currentMicroCodeIdx = CurrentMicroCodeIdx;

                if (OneInstruction)
                {
                    currentInstructionIdx = CodeIdx;
                    currentFlagAreaIdx = tgtCell.RowIndex / LINES_PER_INSTRUCTION;
                }
                else
                {
                    currentInstructionIdx = tgtCell.RowIndex / LINES_PER_INSTRUCTION;
                }
                currentMicroCodeIdx = tgtCell.ColumnIndex - 2;

                switch (tgtCell.RowIndex % LINES_PER_INSTRUCTION)
                {
                    case 0:
                        tgtCell.Value = "";
                        ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].Comment = "";                    
                        break;

                    case 1:
                        tgtCell.Value = cbAdressEnable.Items[0].ToString();
                        ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].AddrOut = 0;
                        break;

                    case 2:
                        tgtCell.Value = cbOutEnable.Items[0].ToString();
                        ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].OutputEn = 0;
                        break;

                    case 3:
                        tgtCell.Value = cbLoadCode.Items[0].ToString();
                        ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].LoadSel = 0;
                        break;

                    case 4:
                        tgtCell.Value = cbAluCode.Items[0].ToString();
                        ROM.MicroCodes[currentSectionIdx][currentFlagAreaIdx][currentInstructionIdx][currentMicroCodeIdx].AluCode = 0;
                        break;
                }
            }
           // dgvCode.ClearSelection();
        }


        private void tsmiPaste_Click(object sender, EventArgs e)
        {
            if ((selectedCells != null) && (selectedCells.Length > 0) && (currentCell != null) && (currentCell.ColumnIndex >= 2))
            {
                try
                {
                    int colMin = int.MaxValue;
                    int rowMin = int.MaxValue;
                    foreach (DataGridViewCell cell in selectedCells)
                    {
                        colMin = Math.Min(colMin, cell.ColumnIndex);
                        rowMin = Math.Min(rowMin, cell.RowIndex);
                    }

                    PasteSelected(colMin, rowMin, false);

                    if (cutMode)
                        ClearSelected();

                    PasteSelected(colMin, rowMin, true);
                }
                catch
                {
                    MessageBox.Show("Cannot Paste the selected cells here!");
                }

            }
        }

    }
}
