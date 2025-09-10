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
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MicroCodeGenerator
{
    /// <summary>
    /// Class for storing and handling micro code information.
    /// </summary>
    internal class MicroCode
    {
        #region Public Constants
        /// <summary>Number of bits for the address enable fields</summary>
        public const int ADDR_ENABLE_BITS = 3;
        /// <summary>Left shift of the bits for the address enable fields</summary>
        public const int ADDR_ENABLE_SHIFT = 0;
        /// <summary>Bit mask for the address enable fields</summary>
        public const int ADDR_ENABLE_MASK = 0x7;

        /// <summary>Number of bits for the output enable fields</summary>
        public const int OUTPUT_ENABLE_BITS = 5;
        /// <summary>Left shift of the bits for the output enable fields</summary>
        public const int OUTPUT_ENABLE_SHIFT = 3;
        /// <summary>Bit mask for the output enable fields</summary>
        public const int OUTPUT_ENABLE_MASK = 0x1F;


        /// <summary>Number of bits for the load select fields</summary>
        public const int LOAD_SEL_BITS = 4;
        /// <summary>Left shift of the bits for the load select fields</summary>
        public const int LOAD_SEL_SHIFT = 8;
        /// <summary>Bit mask for the load select fields</summary>
        public const int LOAD_SEL_MASK = 0xF;

        /// <summary>Number of bits for the ALU code fields</summary>
        public const int ALU_CODE_BITS = 4;
        /// <summary>Left shift of the bits for the ALU code fields</summary>
        public const int ALU_CODE_SHIFT = 4 + 8;
        /// <summary>Bit mask for the ALU code fields</summary>
        public const int ALU_CODE_MASK = 0xF;
        #endregion Public Constants

        #region Public Fields
        public string OpCode;
        public string Mnemonic;
        public string AddrMode;
        public string Description;
        public string Bytes;
        public string Cycles;
        public string Comment;
        #endregion Public Fields

        #region Private Fields
        private int addrOut;
        private int outputEn;
        private int loadSel;
        private int aluCode;
        #endregion Private Fields

        /// <summary>
        /// Creates the instance of MicroCode extracting the contents from the Code.
        /// </summary>
        /// <param name="Code">Code value to assign.</param>
        public MicroCode(int Code)
        {
            this.Code = Code;
        }

        //private void Recode()
        //{ 
        //    //             0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,
        //    int[] recode = {
        //    //0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15  16  17  18  19  20  21  22  23  24  25  26  27  28  29  30  31
        //      0,  0,  0,  0,  0,  0,  0,  0,  1,  20, 24, 2,  3,  4,  13, 11, 10, 9,  18, 14, 16, 17, 12, 19, 5,  6,  7,  23, 8,  21, 22, 25   };

        //    for (int i = 0; i < recode.Length; i++)
        //    {
        //        if (recode[i] == outputEn)
        //        {
        //            outputEn = i;
        //            break;
        //        }
        //    }
        //}

        /// <summary>
        /// Copy the contents of this instance to the target instance.
        /// </summary>
        /// <param name="Target">Target object to copy to.</param>
        public void CopyTo(MicroCode Target)
        {
            Target.Code = this.Code;
            Target.OpCode = this.OpCode;
            Target.Mnemonic = this.Mnemonic;
            Target.AddrMode = this.AddrMode;
            Target.Description = this.Description;
            Target.Bytes = this.Bytes;
            Target.Cycles = this.Cycles;
            Target.Comment = this.Comment;
        }

        /// <summary>
        /// Gets or sets the code of this instance. 
        /// </summary>
        public int Code
        {
            get
            {
                int code = (addrOut << ADDR_ENABLE_SHIFT) | (outputEn << OUTPUT_ENABLE_SHIFT) | (loadSel << LOAD_SEL_SHIFT) | (aluCode << ALU_CODE_SHIFT);
                return code;
            }
            set
            {
                addrOut = (value >> ADDR_ENABLE_SHIFT) & ADDR_ENABLE_MASK;
                outputEn = (value >> OUTPUT_ENABLE_SHIFT) & OUTPUT_ENABLE_MASK;
                loadSel = (value >> LOAD_SEL_SHIFT) & LOAD_SEL_MASK;
                aluCode = (value >> ALU_CODE_SHIFT) & ALU_CODE_MASK;

                //Recode();
            }

        }

        /// <summary>
        /// Gets or sets the address out part of the code.
        /// </summary>
        public int AddrOut
        { 
            get { return addrOut; } 
            set 
            {
                if ((value & ~ADDR_ENABLE_MASK) != 0)
                    throw new Exception("New AddrOut value out of range!");
                else addrOut = value;
            }
        }

        /// <summary>
        /// Gets or sets the output enable part of the code.
        /// </summary>
        public int OutputEn
        {
            get { return outputEn; }
            set
            {
                if ((value & ~OUTPUT_ENABLE_MASK) != 0)
                    throw new Exception("New OutputEn value out of range!");
                else outputEn = value;
            }
        }

        /// <summary>
        /// Gets or sets the load select part of the code.
        /// </summary>
        public int LoadSel
        {
            get { return loadSel; }
            set
            {
                if ((value & ~LOAD_SEL_MASK) != 0)
                    throw new Exception("New LoadSel value out of range!");
                else loadSel = value;
            }
        }

        /// <summary>
        /// Gets or sets the ALU code part of the code.
        /// </summary>
        public int AluCode
        {
            get { return aluCode; }
            set
            {
                if ((value & ~ALU_CODE_MASK) != 0)
                    throw new Exception("New AddrOut value out of range!");
                else aluCode = value;
            }
        }



    }
}
