﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using tams4a.Classes;

namespace tams4a.Forms
{
    public partial class FormAnalysis : Form
    {
        private DataTable roads;
        private DataTable treatments;
        private List<CheckBox> checkBoxes = new List<CheckBox>();
        private List<ComboBox> comboBoxFunctionalClassifications = new List<ComboBox>();
        private List<ComboBox> comboBoxTreatments = new List<ComboBox>();
        private Dictionary<int, string> rowQueries = new Dictionary<int, string>();
        private Dictionary<NumericUpDown, decimal> costBreakdown = new Dictionary<NumericUpDown, decimal>();
        private Dictionary<NumericUpDown, decimal> areaBreakdown = new Dictionary<NumericUpDown, decimal>();
        private TamsProject Project;
        private int numberOfRows = 1;
        private double estBudget = 0.00;
        private bool beingHandled = false;
        public Dictionary<string, double> pricePerYard = new Dictionary<string, double>();
        private ModuleRoads moduleRoads;

        public FormAnalysis(TamsProject theProject, ModuleRoads modRoads)
        {
            InitializeComponent();
            AnalysisRowPanel newPanel = new AnalysisRowPanel(0, "1");
            panelRows.Controls.Add(newPanel);
            Project = theProject;
            moduleRoads = modRoads;
            roads = Database.GetDataByQuery(Project.conn, "SELECT rsl, width, length, type FROM road GROUP BY TAMSID;");
            treatments = Database.GetDataByQuery(Project.conn, "SELECT id, name, cost FROM treatments;");
            pricePerYard.Add("", 0.0);
            pricePerYard.Add("Crack Seal", Convert.ToDouble(treatments.Rows[0]["cost"]));
            pricePerYard.Add("Fog Coat", Convert.ToDouble(treatments.Rows[4]["cost"]));
            pricePerYard.Add("High Mineral Asphalt Emulsion", Convert.ToDouble(treatments.Rows[5]["cost"]));
            pricePerYard.Add("Sand Seal", Convert.ToDouble(treatments.Rows[6]["cost"]));
            pricePerYard.Add("Scrub Seal", Convert.ToDouble(treatments.Rows[7]["cost"]));
            pricePerYard.Add("Single Chip Seal", Convert.ToDouble(treatments.Rows[8]["cost"]));
            pricePerYard.Add("Slurry Seal", Convert.ToDouble(treatments.Rows[9]["cost"]));
            pricePerYard.Add("Microsurfacing", Convert.ToDouble(treatments.Rows[10]["cost"]));
            pricePerYard.Add("Plant Mix Seal", Convert.ToDouble(treatments.Rows[11]["cost"]));
            pricePerYard.Add("Cold In-place Recycling (2 in. with chip seal)", Convert.ToDouble(treatments.Rows[12]["cost"]));
            pricePerYard.Add("Thin Hot Mix Overlay (<2 in.)", Convert.ToDouble(treatments.Rows[13]["cost"]));
            pricePerYard.Add("HMA (leveling) & Overlay (<2 in.)", Convert.ToDouble(treatments.Rows[14]["cost"]));
            pricePerYard.Add("Hot Surface Recycling", Convert.ToDouble(treatments.Rows[15]["cost"]));
            pricePerYard.Add("Rotomill & Overlay (<2 in.)", Convert.ToDouble(treatments.Rows[16]["cost"]));
            pricePerYard.Add("Cold In-place Recycling (2/2 in.)", Convert.ToDouble(treatments.Rows[17]["cost"]));
            pricePerYard.Add("Thick Overlay (3 in.)", Convert.ToDouble(treatments.Rows[18]["cost"]));
            pricePerYard.Add("Rotomill & Thick Overlay (3 in.)", Convert.ToDouble(treatments.Rows[19]["cost"]));
            pricePerYard.Add("Base Repair/ Pavement Replacement", Convert.ToDouble(treatments.Rows[20]["cost"]));
            pricePerYard.Add("Full Depth Reclamation & Overlay (3/3 in.)", Convert.ToDouble(treatments.Rows[21]["cost"]));
            pricePerYard.Add("Base/ Pavement Replacement (3/3/6 in.)", Convert.ToDouble(treatments.Rows[22]["cost"]));
            pricePerYard.Add("Cold Recycling & Overlay (3/3 in.)", Convert.ToDouble(treatments.Rows[23]["cost"]));
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            clearBudgetControlTable();
            costBreakdown.Clear();
            areaBreakdown.Clear();
            comboBoxResultsRow.SelectedIndex = 0;
            buttonFullRowData.Enabled = true;

            double totalArea = 0;
            double totalCost = 0;

            int i = 0;
            foreach (AnalysisRowPanel rowPanel in panelRows.Controls)
            {
                rowQueries[i] = "SELECT * FROM road WHERE rsl >= " + rowPanel.getFromRSL() + " AND rsl <= " + rowPanel.getToRSL();
                string query = "SELECT width, length, rsl FROM road WHERE rsl >= " + rowPanel.getFromRSL() + " AND rsl <= " + rowPanel.getToRSL();
                if (!string.IsNullOrEmpty(rowPanel.getFunctionalClassification()))
                {
                    rowQueries[i] += " AND type = '" + rowPanel.getFunctionalClassification() + "';";
                    query += " AND type = '" + rowPanel.getFunctionalClassification() + "';";
                }
                else
                {
                    rowQueries[i] += ";";
                    query += ";";
                }

                DataTable rslAreas = Database.GetDataByQuery(Project.conn, rowQueries[i]);
                rowPanel.initRSLAreas();
                foreach (DataRow row in rslAreas.Rows)
                {
                    double area = Util.ToDouble(row["width"].ToString()) * Util.ToDouble(row["length"].ToString());
                    rowPanel.setRSLArea(Util.ToInt(row["rsl"].ToString()), area);
                    totalArea += area;
                    totalCost += pricePerYard[rowPanel.getTreatment()] * (area / 9);
                }
                i++;
            }
            
            double roundedCost = Math.Round(totalCost, 2);

            if (roundedCost > estBudget)
            {
                labelOverBudget.Text = "$" + String.Format("{0:n0}", (roundedCost - estBudget)) + " over budget!"; 
                labelOverBudget.Visible = true;
            }
            else
            {
                labelOverBudget.Visible = false;
            }

            textBoxTotalArea.Text = String.Format("{0:n0}", (Math.Round(totalArea/9, 2))) + " yds\u00b2"; 
            textBoxTotalCost.Text = "$" + String.Format("{0:n0}", roundedCost);

            AnalysisRowPanel currentRow = (AnalysisRowPanel)panelRows.Controls[0];
            Dictionary<int, double> rslArea = currentRow.getRSLAreas();
            renderSelectedRowResults(rslArea, currentRow, sender, e);
        }

        private void comboBoxResultsRow_SelectedIndexChanged(object sender, EventArgs e)
        {
            clearBudgetControlTable();
            costBreakdown.Clear();
            areaBreakdown.Clear();
            AnalysisRowPanel currentRow = (AnalysisRowPanel)panelRows.Controls[comboBoxResultsRow.SelectedIndex];
            Dictionary<int, double> rslArea = currentRow.getRSLAreas();
            renderSelectedRowResults(rslArea, currentRow, sender, e);
        }

        private void renderSelectedRowResults(Dictionary<int, double> rslArea, AnalysisRowPanel currentRow, object sender, EventArgs e)
        {

            foreach (int i in rslArea.Keys)
            {
                if (rslArea[i] > 0)
                {
                    RowStyle temp = tableBudgetControl.RowStyles[0];
                    tableBudgetControl.Height += (int)(temp.Height - 20);
                    tableBudgetControl.RowStyles.Add(new RowStyle(temp.SizeType, temp.Height));
                    tableBudgetControl.Controls.Add(new TextBox() { Text = i.ToString(), ReadOnly = true }, 0, tableBudgetControl.RowCount++);
                    NumericUpDown budgetUpDown = new NumericUpDown()
                    {
                        Increment = 100,
                        Minimum = 0,
                        Maximum = (decimal)(pricePerYard[currentRow.getTreatment()] * (rslArea[i] / 9)),
                        Value = (decimal)(pricePerYard[currentRow.getTreatment()] * (rslArea[i] / 9)),
                    };
                    NumericUpDown areaUpDown = new NumericUpDown()
                    {
                        Increment = 100,
                        Minimum = 0,
                        Maximum = (decimal)(rslArea[i] / 9),
                        Value = (decimal)(rslArea[i] / 9)
                    };
                    NumericUpDown percentCoveredUpDown = new NumericUpDown()
                    {
                        Increment = 5,
                        Minimum = 0,
                        Maximum = 100,
                        Value = 100
                    };

                    costBreakdown[budgetUpDown] = budgetUpDown.Value;
                    areaBreakdown[areaUpDown] = areaUpDown.Value;
                    double costPerYard = (double)(budgetUpDown.Value / areaUpDown.Value);
                    budgetUpDown.ValueChanged += new EventHandler(delegate (object _sender, EventArgs _e) { BudgetUpDown_ValueChanged(sender, e, costPerYard, budgetUpDown, areaUpDown, percentCoveredUpDown); });
                    areaUpDown.ValueChanged += new EventHandler(delegate (object _sender, EventArgs _e) { AreaUpDown_ValueChanged(sender, e, costPerYard, budgetUpDown, areaUpDown, percentCoveredUpDown); });
                    percentCoveredUpDown.ValueChanged += new EventHandler(delegate (object _sender, EventArgs _e) { PercentCoveredUpDown_ValueChanged(sender, e, costPerYard, budgetUpDown, areaUpDown, percentCoveredUpDown); });
                    tableBudgetControl.Controls.Add(budgetUpDown, 1, tableBudgetControl.RowCount - 1);
                    tableBudgetControl.Controls.Add(areaUpDown, 2, tableBudgetControl.RowCount - 1);
                    tableBudgetControl.Controls.Add(percentCoveredUpDown, 3, tableBudgetControl.RowCount - 1);
                }
            }
            tableBudgetControl.Visible = true;
            //for (int i = 0; i <= 20; i++)
            //{
            //    rslArea[i] = 0;
            //}
        }

        private void BudgetUpDown_ValueChanged(object sender, EventArgs e, double costPerYard, NumericUpDown budgetUpDown, NumericUpDown areaUpDown, NumericUpDown percentCoveredUpDown)
        {
            handleNumericUpDownChanges(costPerYard, budgetUpDown, areaUpDown, percentCoveredUpDown, "budget");
        }

        private void AreaUpDown_ValueChanged(object sender, EventArgs e, double costPerYard, NumericUpDown budgetUpDown, NumericUpDown areaUpDown, NumericUpDown percentCoveredUpDown)
        {
            handleNumericUpDownChanges(costPerYard, budgetUpDown, areaUpDown, percentCoveredUpDown, "area");
        }

        private void PercentCoveredUpDown_ValueChanged(object sender, EventArgs e, double costPerYard, NumericUpDown budgetUpDown, NumericUpDown areaUpDown, NumericUpDown percentCoveredUpDown)
        {
            handleNumericUpDownChanges(costPerYard, budgetUpDown, areaUpDown, percentCoveredUpDown, "percent");
        }

        private void handleNumericUpDownChanges(double costPerYard, NumericUpDown budgetUpDown, NumericUpDown areaUpDown, NumericUpDown percentCoveredUpDown, string caller)
        {
            if (beingHandled) return;
            beingHandled = true;

            if (caller == "area")
            {
                decimal newCost = (decimal)((double)areaUpDown.Value * costPerYard);
                decimal newPercentCovered = (decimal)(((double)areaUpDown.Value / (double)areaUpDown.Maximum) * 100);
                budgetUpDown.Value = newCost;
                percentCoveredUpDown.Value = newPercentCovered;
            }

            if (caller == "budget")
            {
                decimal newArea = (decimal)((double)budgetUpDown.Value / costPerYard);
                decimal newPercentCovered = (decimal)(((double)newArea / (double)areaUpDown.Maximum) * 100);
                areaUpDown.Value = newArea;
                percentCoveredUpDown.Value = newPercentCovered;
            }

            if (caller == "percent")
            {
                decimal newArea = (decimal)((double)areaUpDown.Maximum * (((double)percentCoveredUpDown.Value) / 100));
                decimal newCost = (decimal)((double)newArea * costPerYard);
                areaUpDown.Value = newArea;
                budgetUpDown.Value = newCost;
            }

            areaBreakdown[areaUpDown] = areaUpDown.Value;
            decimal totalArea = 0;
            foreach (decimal area in areaBreakdown.Values)
            {
                totalArea += area;
            }
            textBoxTotalArea.Text = String.Format("{0:n0}", (Math.Round(totalArea, 2))) + " yds\u00b2";

            costBreakdown[budgetUpDown] = budgetUpDown.Value;
            decimal totalCost = 0;
            foreach (decimal price in costBreakdown.Values)
            {
                totalCost += price;
            }
            textBoxTotalCost.Text = "$" + String.Format("{0:n0}", totalCost); ;
            if ((double)totalCost > estBudget)
            {
                labelOverBudget.Text = "$" + String.Format("{0:n0}", ((double)totalCost - estBudget)) + " over budget!";
                labelOverBudget.Visible = true;
            }
            else
            {
                labelOverBudget.Visible = false;
            }

            beingHandled = false;
        }

        private void clearBudgetControlTable()
        {
            tableBudgetControl.Visible = false;
            tableBudgetControl.RowCount = 1;
            tableBudgetControl.Controls.Clear();
            tableBudgetControl.RowStyles.Clear();
            tableBudgetControl.ColumnCount = 4;
            tableBudgetControl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            tableBudgetControl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            tableBudgetControl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            tableBudgetControl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            tableBudgetControl.Controls.Add(labelRSLx, 0, 0);
            tableBudgetControl.Controls.Add(labelBudgetUsed, 1, 0);
            tableBudgetControl.Controls.Add(labelAreaCovered, 2, 0);
            tableBudgetControl.Controls.Add(labelPercentConvered, 3, 0);
            tableBudgetControl.Location = new System.Drawing.Point(14, 119);
            tableBudgetControl.Name = "tableBudgetControl";
            tableBudgetControl.RowCount = 1;
            tableBudgetControl.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableBudgetControl.Size = new System.Drawing.Size(300, 15);
            tableBudgetControl.TabIndex = 32;
            tableBudgetControl.AutoScroll = true;
        }

        private void textBoxBudget_RemovePlaceholder(object sender, EventArgs e)
        {
            if (estBudget != 0.0)
            {
                textBoxBudget.Text = estBudget.ToString();
                return;
            }
            textBoxBudget.Text = "";
        }

        private void textBoxBudget_AddPlaceholder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxBudget.Text))
            {
                estBudget = 0.0;
                textBoxBudget.Text = "$0.00";
            }
            else
            {
                estBudget = Util.ToDouble(textBoxBudget.Text);
                textBoxBudget.Text = "$" + String.Format("{0:n0}", estBudget);
            }
            buttonCalculate_Click(sender, null);
        }

        private void textBoxBudget_EnterPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                panelCalculator.Focus();
            }
        }

        private void buttonTreatmentCosts_Click(object sender, EventArgs e)
        {
            FormTreatmentCosts treatmentCosts = new FormTreatmentCosts(Project, pricePerYard, this);
            treatmentCosts.ShowDialog();
        }

        private void buttonAddRow_Click(object sender, EventArgs e)
        {
            AnalysisRowPanel newPanel = new AnalysisRowPanel(28 * numberOfRows, (numberOfRows + 1).ToString());
            panelRows.Controls.Add(newPanel);
            numberOfRows++;
            comboBoxResultsRow.Items.AddRange(new object[] { numberOfRows.ToString() });
            rowQueries[numberOfRows - 1] = "";
            buttonRemoveRow.Enabled = true;
            if (numberOfRows == 23) buttonAddRow.Enabled = false;
        }

        private void buttonDeleteRow_Click(object sender, EventArgs e)
        {
            numberOfRows--;
            panelRows.Controls.RemoveAt(numberOfRows);
            buttonAddRow.Enabled = true;
            comboBoxResultsRow.Items.RemoveAt(numberOfRows);
            if (numberOfRows == 1) buttonRemoveRow.Enabled = false;
        }

        private void buttonFullRowData_Click(object sender, EventArgs e)
        {
            FormOutput report = new FormOutput(Project, moduleRoads);
            report.dataGridViewReport.DataSource = Database.GetDataByQuery(Project.conn, rowQueries[comboBoxResultsRow.SelectedIndex]);
            report.Text = "Full Report of Selected Row";
            report.Show();
        }
    }
}