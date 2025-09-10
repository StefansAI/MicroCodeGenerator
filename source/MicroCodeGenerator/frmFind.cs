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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MicroCodeGenerator
{
    /// <summary>
    /// Find form class.
    /// </summary>
    public partial class frmFind : Form
    {
        /// <summary>Reference to the main form.</summary>
        private frmMain frmMain;

        /// <summary>
        /// Creates the instance of the find form. 
        /// </summary>
        /// <param name="frmMain">Reference to the main form.</param>
        public frmFind(frmMain frmMain)
        {
            InitializeComponent();
            this.frmMain = frmMain;
        }

        /// <summary>
        /// Form shown event handler.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void frmFind_Shown(object sender, EventArgs e)
        {
            tbFindText.Focus();
        }

        /// <summary>
        /// Form closing event handler.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void frmFind_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (frmMain != null)
            //    frmMain.frmFind = null;
        }

        /// <summary>
        /// Ok button click event handler.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void btnOK_Click(object sender, EventArgs e)
        {
           
        }

        /// <summary>
        /// Cancel button click event handler.
        /// </summary>
        /// <param name="sender">Reference to the sender object.</param>
        /// <param name="e">Event argument passed with the call.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Gets the entered text to find.
        /// </summary>
        public string FindText
        {
            get { return tbFindText.Text; }
        }


    }
}
