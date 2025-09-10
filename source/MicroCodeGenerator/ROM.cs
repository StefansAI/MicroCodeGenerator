// ================================================
//
// SPDX-FileCopyrightText: 2024/25 Stefan Warnke
//
// SPDX-License-Identifier: BeerWare
//
//=================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace MicroCodeGenerator
{
    /// <summary>
    /// Class definition of the combined ROM contents structured according the address bit groups.
    /// </summary>
    internal class ROM
    {
        #region Public Constants
        /// <summary>Number of available micro codes for each instructions.</summary>
        public const int MICRO_CODES_PER_INSTRUCTION = 32;
        /// <summary>Number of individual instructions.</summary>
        public const int NO_OF_INSTRUCTIONS = 256;
        /// <summary>Number of areas for all flag combinations.</summary>
        public const int FLAG_AREA_COUNT = 8;
        /// <summary>Number of global sections, one for all opcodes and one for all exceptions.</summary>
        public const int NO_OF_GLOBAL_SECTIONS = 2;
        /// <summary>SHift of the higher ROM file to create a 16 bit wide code.</summary>
        public const int BYTE_SHIFT = 8;
        /// <summary>Total number of address bits of the ROMs.</summary>
        public const int ROM_ADDRESS_BITS = 17;
        #endregion Public Constants

        #region Private Fields
        /// <summary>Multi-dimensonal micro code array structured as sections, flag areas, instructions, micro codes per instruction</summary>
        private MicroCode[][][][] microCodes;
        /// <summary>File name of the lower micro code ROM to be loaded into microCodes.</summary>
        private string LowRomFileName;
        /// <summary>File name of the upper micro code ROM to be loaded into microCodes.</summary>
        private string HighRomFileName;
        /// <summary>File name of the text file containing the comments for all micro codes.</summary>
        private string CommentFileName;
        #endregion Private Fields

        /// <summary>
        /// Creates the instance of the ROM class. It creates the microCodes array and loads the contents from the files.
        /// </summary>
        /// <param name="LowRomFileName">File name of the lower micro code ROM to be loaded into microCodes.</param>
        /// <param name="HighRomFileName">File name of the upper micro code ROM to be loaded into microCodes.</param>
        /// <param name="CommentFileName">File name of the text file containing the comments for all micro codes.</param>
        public ROM(string LowRomFileName, string HighRomFileName, string CommentFileName)
        {
            this.LowRomFileName = LowRomFileName;
            this.HighRomFileName = HighRomFileName;
            this.CommentFileName = CommentFileName;

            microCodes = new MicroCode[NO_OF_GLOBAL_SECTIONS][][][];
            for (int s = 0; s < NO_OF_GLOBAL_SECTIONS; s++)
            {
                microCodes[s] = new MicroCode[FLAG_AREA_COUNT][][];
                for (int f = 0; f < FLAG_AREA_COUNT; f++)
                {
                    microCodes[s][f] = new MicroCode[NO_OF_INSTRUCTIONS][];
                    for (int i = 0; i < NO_OF_INSTRUCTIONS; i++)
                    {
                        microCodes[s][f][i] = new MicroCode[MICRO_CODES_PER_INSTRUCTION];
                        for (int m = 0; m < MICRO_CODES_PER_INSTRUCTION; m++)
                            microCodes[s][f][i][m] = new MicroCode(0);
                    }
                }
            }
            LoadFromFiles(LowRomFileName, HighRomFileName, CommentFileName);
        }

        /// <summary>
        /// Loads the contents from the 3 files into the microCodes.
        /// </summary>
        public void LoadFromFiles()
        {
            LoadFromFiles(this.LowRomFileName, this.HighRomFileName, this.CommentFileName);
        }

        /// <summary>
        /// Loads the contents from the 3 files into the microCodes.
        /// </summary>
        /// <param name="LowRomFileName">File name of the lower micro code ROM to be loaded into microCodes.</param>
        /// <param name="HighRomFileName">File name of the upper micro code ROM to be loaded into microCodes.</param>
        /// <param name="CommentFileName">File name of the text file containing the comments for all micro codes.</param>
        public void LoadFromFiles(string LowRomFileName, string HighRomFileName, string CommentFileName)
        {
            if (File.Exists(LowRomFileName) && File.Exists(HighRomFileName) && File.Exists(CommentFileName))
            {
                this.LowRomFileName = LowRomFileName;
                this.HighRomFileName = HighRomFileName;
                this.CommentFileName = CommentFileName;

                byte[] lowROM = File.ReadAllBytes(LowRomFileName);
                byte[] highROM = File.ReadAllBytes(HighRomFileName);
                StreamReader sr = new StreamReader(CommentFileName);
                sr.ReadLine();

                int idx = 0;
                for (int s = 0; s < NO_OF_GLOBAL_SECTIONS; s++)
                    for (int f = 0; f < FLAG_AREA_COUNT; f++)
                        for (int i = 0; i < NO_OF_INSTRUCTIONS; i++)
                        {
                            string line = sr.ReadLine();
                            string[] ss = line.Split(new char[] { '\t' });
                            for (int m = 0; m < MICRO_CODES_PER_INSTRUCTION; m++, idx++)
                            {
                                microCodes[s][f][i][m].Code = (highROM[idx] << BYTE_SHIFT) | lowROM[idx];
                                try { microCodes[s][f][i][m].Comment = ss[6 + m]; }
                                catch { microCodes[s][f][i][m].Comment = ""; }
                            }
                        }
                sr.Close();
            }
        }

        /// <summary>
        /// Saves the current contents of microCodes to the three files.
        /// </summary>
        public void SaveToFiles()
        {
            SaveToFiles(this.LowRomFileName, this.HighRomFileName, this.CommentFileName);
        }

        /// <summary>
        /// Saves the current contents of microCodes to the three files.
        /// </summary>
        /// <param name="LowRomFileName">File name of the lower micro code ROM to be loaded into microCodes.</param>
        /// <param name="HighRomFileName">File name of the upper micro code ROM to be loaded into microCodes.</param>
        /// <param name="CommentFileName">File name of the text file containing the comments for all micro codes.</param>
        public void SaveToFiles(string LowRomFileName, string HighRomFileName, string CommentFileName)
        {
            byte[] lowROM = new byte[1 << ROM_ADDRESS_BITS];
            byte[] highROM = new byte[1 << ROM_ADDRESS_BITS];
            StreamWriter sw = new StreamWriter(CommentFileName);
            sw.WriteLine("OpCode\tMnemonic\tAddrMode\tDescription\tBytes\tCycles\tT0_0\tT0_1\tT1_0\tT1_1\tT2_0\tT2_1\tT3_0\tT3_1\tT4_0\tT4_1\tT5_0\tT5_1\tT6_0\tT6_1\tT7_0\tT7_1\tT8_0\tT8_1\tT9_0\tT9_1\tT10_0\tT10_1\tT11_0\tT11_1\tT12_0\tT12_1\tT13_0\tT13_1\tT14_0\tT14_1\tT15_0\tT15_1");

            int idx = 0;
            for (int s = 0; s < NO_OF_GLOBAL_SECTIONS; s++)
                for (int f = 0; f < FLAG_AREA_COUNT; f++)
                    for (int i = 0; i < NO_OF_INSTRUCTIONS; i++)
                    {
                        sw.Write(microCodes[s][f][i][0].OpCode + "\t" + microCodes[s][f][i][0].Mnemonic + "\t" + microCodes[s][f][i][0].AddrMode + "\t" + microCodes[s][f][i][0].Description + "\t" + microCodes[s][f][i][0].Bytes + "\t" + microCodes[s][f][i][0].Cycles);
                        for (int m = 0; m < MICRO_CODES_PER_INSTRUCTION; m++, idx++)
                        {
                            int code = microCodes[s][f][i][m].Code;
                            lowROM[idx] = (byte)(code & 0xFF);
                            highROM[idx] = (byte)((code >> BYTE_SHIFT) & 0xFF);
                            sw.Write("\t"+microCodes[s][f][i][m].Comment);
                        }
                        sw.WriteLine();
                    }

            sw.Close();
            File.WriteAllBytes(LowRomFileName, lowROM);
            File.WriteAllBytes(HighRomFileName, highROM);
        }

        /// <summary>
        /// Gets the reference to the micro codde arrays.
        /// </summary>
        public MicroCode[][][][] MicroCodes
        {
            get { return microCodes;  }
        }
    }
}
