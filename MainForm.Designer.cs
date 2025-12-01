using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTDMapgen_WinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Панели
        private Panel toolPanel;
        private Panel propertiesPanel;
        private Panel displayPanel;

        // Кнопки инструментов
        private Button btnSelect;
        private Button btnBrush;
        private Button btnMountain;

        // Кнопки файлов
        private Button btnNew;
        private Button btnOpen;
        private Button btnSave;
        private Button btnUndo;
        private Button btnRedo;

        // Настройки отображения
        private CheckBox chkApplyHills;
        private ComboBox cmbHeightMode;
        private ComboBox cmbDisplayMode;
        private NumericUpDown numWaterLevel;

        // Свойства террейна
        private NumericUpDown numBaseHeight;
        private NumericUpDown numHeightScale;
        private NumericUpDown numBiome;
        private Label lblBaseHeightMeters;
        private Label lblHeightScaleMeters;
        private Label lblRawHeight;
        private Label lblBaseHeightMeters2;
        private Label lblHeightScaleMeters2;
        private Label lblRawHeight2;
        private Label lblEditorMode;

        // Свойства горы
        private NumericUpDown numMountainX;
        private NumericUpDown numMountainY;
        private NumericUpDown numMountainRadius;
        private NumericUpDown numCenterBaseMod;
        private NumericUpDown numBorderBaseMod;
        private NumericUpDown numCenterScaleMod;
        private NumericUpDown numBorderScaleMod;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.Text = "Terrain Editor";
            this.DoubleBuffered = true;
            this.BackColor = System.Drawing.Color.White;

            CreateToolPanel();
            CreatePropertiesPanel();
            CreateDisplayPanel();

            this.ResumeLayout(false);
        }

        private void CreateToolPanel()
        {
            toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightGray
            };

            // Кнопки файлов
            btnNew = CreateButton("New", 10);
            btnOpen = CreateButton("Open", 60);
            btnSave = CreateButton("Save", 110);
            btnUndo = CreateButton("Undo", 160);
            btnRedo = CreateButton("Redo", 210);

            // Кнопки инструментов
            btnSelect = CreateButton("Select", 260);
            btnBrush = CreateButton("Brush", 310);
            btnMountain = CreateButton("Mountain", 360);

            toolPanel.Controls.AddRange(new Control[] {
            btnNew, btnOpen, btnSave, btnUndo, btnRedo,
            btnSelect, btnBrush, btnMountain
            });

            this.Controls.Add(toolPanel);//в конец?

            Button btnFitToView = CreateButton("Fit", 410);
            btnFitToView.Click += (s, e) => FitToView();

            toolPanel.Controls.Add(btnFitToView);
        }

        private void CreatePropertiesPanel()
        {
            propertiesPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 300,
                BackColor = SystemColors.Control
            };

            // Здесь будет создание элементов управления для свойств
            // (ограничение размера ответа не позволяет включить полный код)

            this.Controls.Add(propertiesPanel);
        }

        private void CreateDisplayPanel()
        {
            /*displayPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var displayGroup = new GroupBox
            {
                Text = "Display Settings",
                Location = new Point(10, 10),
                Size = new Size(280, 120)
            };*/

            displayPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var displayGroup = new GroupBox
            {
                Text = "Display Settings",
                Location = new Point(10, 10),
                Size = new Size(280, 120)
            };

            chkApplyHills = new CheckBox { Text = "Apply Hills", Location = new Point(10, 20), Checked = true };

            cmbHeightMode = new ComboBox
            {
                Location = new Point(10, 45),
                Size = new Size(120, 21),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbHeightMode.Items.AddRange(new[] { "Average", "Max", "Min", "Raw", "Straight" });
            cmbHeightMode.SelectedIndex = 0;

            cmbDisplayMode = new ComboBox
            {
                Location = new Point(140, 45),
                Size = new Size(120, 21),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDisplayMode.Items.AddRange(new[] { "Flat", "Gradient" });
            cmbDisplayMode.SelectedIndex = 0;

            // ИСПРАВЛЕНИЕ: Укороченный label и сдвинутое поле
            var lblWaterLevel = new Label
            {
                Text = "Water:",  // Укорочили текст
                Location = new Point(10, 75),
                Size = new Size(40, 20)
            };

            numWaterLevel = new NumericUpDown
            {
                Location = new Point(55, 73),  // Сдвинули правее
                Size = new Size(60, 20),
                Value = 5,
                Minimum = -500,
                Maximum = 500,
                DecimalPlaces = 1,
                Increment = 0.1m
            };

            displayGroup.Controls.AddRange(new Control[] {
        chkApplyHills, cmbHeightMode, cmbDisplayMode, lblWaterLevel, numWaterLevel
            });

            propertiesPanel.Controls.Add(displayGroup);

            // ДОБАВЛЯЕМ ПАНЕЛЬ СВОЙСТВ ТЕРРЕЙНА
            //CreateTerrainPropertiesPanel();
            //propertiesPanel.Controls.Add(displayGroup);

            // СОЗДАЕМ ВСЕ ПАНЕЛИ СВОЙСТВ
            CreateTerrainPropertiesPanel();
            CreateMountainPropertiesPanel();
        }

        public string getEditorText()
        {
            string editorMS = "Editor Mode: UNDEFINED LMAO";
            if (currentMode == EditorMode.Select) editorMS = "Editor Mode: editing selection";
            if (currentMode == EditorMode.Brush) editorMS = "Editor Mode: terrain brush";
            return editorMS;
        }

        private Button CreateButton(string text, int x)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 8),
                Size = new Size(45, 25)
            };
        }

        private void CreateTerrainPropertiesPanel()
        {
            var terrainGroup = new GroupBox
            {
                Text = "Terrain Properties",
                Location = new Point(10, 140),
                Size = new Size(280, 200),
                Visible = false
            };

            lblEditorMode = new Label { Text = "nu uh", Location = new Point(10, 0), Width = 300 }; //if var it will tell that this label is null

            // Base Height
            var lblBaseHeight = new Label { Text = "Base Height:", Location = new Point(10, 25) };
            numBaseHeight = new NumericUpDown
            {
                Location = new Point(150, 23), //(100,23)
                Size = new Size(80, 20),
                Minimum = -1.0m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.01m
            };
            numBaseHeight.ValueChanged += (s, e) =>
            {
                if (selectedTerrain != null)
                {
                    selectedTerrain.BaseHeight = (float)numBaseHeight.Value;
                    UpdateMeterValues();
                    Invalidate();
                }

                if (storeTerrain != null)
                {
                    storeTerrain.BaseHeight = (float)numBaseHeight.Value;
                    UpdateMeterValues();
                    Invalidate();
                }
            };

            // Height Scale
            var lblHeightScale = new Label { Text = "Height Scale:", Location = new Point(10, 50) };
            numHeightScale = new NumericUpDown
            {
                Location = new Point(150, 48), //(100, 48)
                Size = new Size(80, 20),
                Minimum = 0.0m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.01m
            };
            numHeightScale.ValueChanged += (s, e) =>
            {
                if (selectedTerrain != null)
                {
                    selectedTerrain.HeightScale = (float)numHeightScale.Value;
                    UpdateMeterValues();
                    Invalidate();
                }

                if (selectedTerrain != null)
                {
                    storeTerrain.HeightScale = (float)numHeightScale.Value;
                    UpdateMeterValues();
                    Invalidate();
                }
            };

            // Biome
            var lblBiome = new Label { Text = "Biome:", Location = new Point(10, 75) };
            numBiome = new NumericUpDown
            {
                Location = new Point(150, 73), //(100, 73)
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 10,
                Increment = 1
            };
            numBiome.ValueChanged += (s, e) =>
            {
                if (selectedTerrain != null)
                {
                    selectedTerrain.Biome = (int)numBiome.Value;
                    Invalidate();
                }

                if (selectedTerrain != null)
                {
                    storeTerrain.Biome = (int)numBiome.Value;
                    Invalidate();
                }
            };

            // Метровые значения
            lblBaseHeightMeters = new Label { Text = "0m", Location = new Point(150, 124), Size = new Size(80, 20) }; //(190, 25)
            lblHeightScaleMeters = new Label { Text = "0m", Location = new Point(150, 148), Size = new Size(80, 20) };//(190, 50)
            lblRawHeight = new Label { Text = "0m", Location = new Point(150, 100), Size = new Size(150, 20) };//(100, 100)

            lblBaseHeightMeters2 = new Label { Text = "BaseHeight:", Location = new Point(10, 124), Size = new Size(80, 20) }; 
            lblHeightScaleMeters2 = new Label { Text = "HeightScale:", Location = new Point(10, 148), Size = new Size(80, 20) };
            lblRawHeight2 = new Label { Text = "RawHeight:", Location = new Point(10, 100), Size = new Size(150, 20) };

            terrainGroup.Controls.AddRange(new Control[] {lblEditorMode,
        lblBaseHeight, numBaseHeight,
        lblHeightScale, numHeightScale,
        lblBiome, numBiome,
        lblBaseHeightMeters, lblHeightScaleMeters, lblRawHeight, lblBaseHeightMeters2, lblHeightScaleMeters2, lblRawHeight2
        });

            propertiesPanel.Controls.Add(terrainGroup);

            // Сохраняем ссылку для показа/скрытия
            terrainPropertiesGroup = terrainGroup;
        }

        private void CreateMountainPropertiesPanel()
        {
            mountainPropertiesGroup = new GroupBox
            {
                Text = "Mountain Properties",
                Location = new Point(10, 350),
                Size = new Size(280, 250),
                Visible = false
            };

            // Позиция X
            var lblMountainX = new Label { Text = "Position X:", Location = new Point(10, 25) };
            numMountainX = new NumericUpDown
            {
                Location = new Point(150, 23), //(100, 23)
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 10000,
                DecimalPlaces = 1,
                Increment = 10m
            };
            numMountainX.ValueChanged += (s, e) =>
            {
                if (selectedMountain != null)
                {
                    selectedMountain.Position = new PointF((float)numMountainX.Value, selectedMountain.Position.Y);
                    Invalidate();
                }
            };

            // Позиция Y
            var lblMountainY = new Label { Text = "Position Y:", Location = new Point(10, 50) };
            numMountainY = new NumericUpDown
            {
                Location = new Point(150, 48), //(100, 48)
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 10000,
                DecimalPlaces = 1,
                Increment = 10m
            };
            numMountainY.ValueChanged += (s, e) =>
            {
                if (selectedMountain != null)
                {
                    selectedMountain.Position = new PointF(selectedMountain.Position.X, (float)numMountainY.Value);
                    Invalidate();
                }
            };

            // Радиус
            var lblMountainRadius = new Label { Text = "Radius:", Location = new Point(10, 75) };
            numMountainRadius = new NumericUpDown
            {
                Location = new Point(150, 73), //(100, 73)
                Size = new Size(80, 20),
                Minimum = 10,
                Maximum = 5000,
                DecimalPlaces = 1,
                Increment = 10m
            };
            numMountainRadius.ValueChanged += (s, e) =>
            {
                if (selectedMountain != null)
                {
                    selectedMountain.Radius = (float)numMountainRadius.Value;
                    Invalidate();
                }
            };

            // Модификаторы высоты
            var lblCenterBaseMod = new Label { Text = "Center Base Mod:", Location = new Point(10, 100) };
            numCenterBaseMod = new NumericUpDown
            {
                Location = new Point(120, 98),
                Size = new Size(60, 20),
                Minimum = -1.0m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.01m
            };

            var lblBorderBaseMod = new Label { Text = "Border Base Mod:", Location = new Point(10, 125) };
            numBorderBaseMod = new NumericUpDown
            {
                Location = new Point(120, 123),
                Size = new Size(60, 20),
                Minimum = -1.0m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.01m
            };

            var lblCenterScaleMod = new Label { Text = "Center Scale Mod:", Location = new Point(10, 150) };
            numCenterScaleMod = new NumericUpDown
            {
                Location = new Point(120, 148),
                Size = new Size(60, 20),
                Minimum = -1.0m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.01m
            };

            var lblBorderScaleMod = new Label { Text = "Border Scale Mod:", Location = new Point(10, 175) };
            numBorderScaleMod = new NumericUpDown
            {
                Location = new Point(120, 173),
                Size = new Size(60, 20),
                Minimum = -1.0m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.01m
            };

            // Обработчики изменений модификаторов
            numCenterBaseMod.ValueChanged += MountainPropertyChanged;
            numBorderBaseMod.ValueChanged += MountainPropertyChanged;
            numCenterScaleMod.ValueChanged += MountainPropertyChanged;
            numBorderScaleMod.ValueChanged += MountainPropertyChanged;

            mountainPropertiesGroup.Controls.AddRange(new Control[] {
        lblMountainX, numMountainX,
        lblMountainY, numMountainY,
        lblMountainRadius, numMountainRadius,
        lblCenterBaseMod, numCenterBaseMod,
        lblBorderBaseMod, numBorderBaseMod,
        lblCenterScaleMod, numCenterScaleMod,
        lblBorderScaleMod, numBorderScaleMod
    });

            propertiesPanel.Controls.Add(mountainPropertiesGroup);
        }

        private void MountainPropertyChanged(object sender, EventArgs e)
        {
            if (selectedMountain != null)
            {
                selectedMountain.CenterBaseHeightMod = (float)numCenterBaseMod.Value;
                selectedMountain.BorderBaseHeightMod = (float)numBorderBaseMod.Value;
                selectedMountain.CenterHeightScaleMod = (float)numCenterScaleMod.Value;
                selectedMountain.BorderHeightScaleMod = (float)numBorderScaleMod.Value;
                Invalidate();
            }
        }
    }
}
