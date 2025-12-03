using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

 

    namespace FTDMapgen_WinForms
    {
        public partial class MainForm : Form
        {
            private WorldData worldData = new WorldData(); // Инициализация по умолчанию
            private Bitmap terrainBitmap;
            private Graphics terrainGraphics;

            // Настройки отображения
            private DisplaySettings displaySettings = new DisplaySettings();
            private List<Mountain> mountains = new List<Mountain>();

            // Система отмены
            private Stack<TerrainAction> undoStack = new Stack<TerrainAction>(); //redundent functional
            private Stack<TerrainAction> redoStack = new Stack<TerrainAction>(); //redundent functional

            public Stack<Action> undoStack2 = new Stack<Action>();
            public Stack<Action> redoStack2 = new Stack<Action>();

            public ChangePointer changePointer = new ChangePointer();

            // Состояние редактора
            private EditorMode currentMode = EditorMode.Select; //cursor mode
            private TerrainBrush currentBrush = new TerrainBrush(); //not used yet
            private Terrain selectedTerrain = null; //currently selected terrain that is modified
            private Mountain selectedMountain = null; //currently selected mountain that is modified
            private Terrain storeTerrain =  new Terrain()
            {
                BaseHeight = 0f,
                HeightScale = 0f,
                Biome = 3,
                Seed = 1, //seed 1 is a bad thing, need a generator
                PerlinFrequency = 4,
                PerlinOctaves = 5
            }; //terrain for brush
            private Mountain storeMountain = null; //mountain for brush
            private bool isDraggingMountain = false;
            private Point lastMousePos;
            private bool isPanning = false;
            private float scale = 1.0f;
            private PointF panOffset = new PointF(0, 0);

            private GroupBox terrainPropertiesGroup;
            private GroupBox mountainPropertiesGroup;

        // Цвета биомов
        private static readonly Dictionary<int, Color> BiomeColors = new Dictionary<int, Color>
            {
                { 0, Color.Gray },     // Неизвестный, ocean floor
                { 1, Color.Green },    // Лес, plains
                { 2, Color.YellowGreen }, // Равнина, desert actually
                { 3, Color.White },    // Снег, snow
                { 4, Color.Brown }    // Горы, Lava
            };

            public MainForm()
            {
                InitializeComponent();
                InitializeWorldData(); // Новая функция инициализации
                InitializeTerrainBitmap();
                SetupEventHandlers();
                FitToView(); // Автоматическое подгонка под вид
            }

            private void InitializeTerrainBitmap()
            {
                terrainBitmap = new Bitmap(3000, 3000);
                terrainGraphics = Graphics.FromImage(terrainBitmap);
                terrainGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            }

            private void SetupEventHandlers()
            {
                // Обработчики масштабирования и панорамирования
                this.MouseWheel += MainForm_MouseWheel;
                this.MouseDown += MainForm_MouseDown;
                this.MouseMove += MainForm_MouseMove;
                this.MouseUp += MainForm_MouseUp;

                // Обработчики кнопок
                btnNew.Click += BtnNew_Click;
                btnOpen.Click += BtnOpen_Click;
                btnSave.Click += BtnSave_Click;
                btnUndo.Click += BtnUndo_Click;
                btnRedo.Click += BtnRedo_Click;
                //Mountain Delete is in designer

                // Обработчики изменения настроек
                cmbHeightMode.SelectedIndexChanged += SettingsChanged;
                cmbDisplayMode.SelectedIndexChanged += SettingsChanged;
                chkApplyHills.CheckedChanged += SettingsChanged;
                numWaterLevel.ValueChanged += SettingsChanged;

                // Обработчики инструментов
                btnSelect.Click += (s, e) => currentMode = EditorMode.Select;
                btnBrush.Click += (s, e) => currentMode = EditorMode.Brush;
                btnMountain.Click += (s, e) => currentMode = EditorMode.Mountain;

                // Обработчики инструментов
                btnSelect.Click += (s, e) =>
                {
                    currentMode = EditorMode.Select;
                    UpdateToolButtons();
                };

                btnBrush.Click += (s, e) =>
                {
                    currentMode = EditorMode.Brush;
                    UpdateToolButtons();
                };

                btnMountain.Click += (s, e) =>
                {
                    currentMode = EditorMode.Mountain;
                    UpdateToolButtons();
                };
            }

            private void InitializeWorldData() //TEMPORARY FOR DEBUG. INITIALIZATION IS INSUFFICIENT
            {
                // Создаем базовую структуру если данных нет
                if (worldData.BoardLayout == null)
                {
                    worldData.BoardLayout = new BoardLayout
                    {
                        BoardSections = new List<List<BoardSection>>(),
                        TerrainsPerBoard = 7,
                        TerrainSize = 256.0f,
                        WorldHeightAndDepth = 500
                    };

                    // Создаем 3x3 больших секций
                    for (int i = 0; i < 3; i++)
                    {
                        var row = new List<BoardSection>();
                        for (int j = 0; j < 3; j++)
                        {
                            var section = new BoardSection
                            {
                                Terrains = new List<List<Terrain>>()
                            };

                            // Создаем 7x7 малых террейнов
                            for (int y = 0; y < 7; y++)
                            {
                                var terrainRow = new List<Terrain>();
                                for (int x = 0; x < 7; x++)
                                {
                                    terrainRow.Add(new Terrain
                                    {
                                        BaseHeight = 0f,
                                        HeightScale = 0f,
                                        Biome = 3,
                                        Seed = 1,
                                        PerlinFrequency = 4,
                                        PerlinOctaves = 5
                                    });
                                }
                                section.Terrains.Add(terrainRow);
                            }
                            row.Add(section);
                        }
                        worldData.BoardLayout.BoardSections.Add(row);
                    }
                }
            }
        

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                if (worldData?.BoardLayout?.BoardSections == null) return;

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.LightGray);

                // Применяем трансформации
                g.TranslateTransform(panOffset.X, panOffset.Y);
                g.ScaleTransform(scale, scale);

                // Рисуем сетку
                //DrawGrid(g);

                // Рисуем террейн
                DrawTerrain(g);
                DrawGrid(g);

                // Рисуем горы
                DrawMountains(g);

                // Рисуем выделения
                DrawSelections(g);
            }

            private void DrawGrid(Graphics g)
            {
                var gridPen = new Pen(Color.FromArgb(50, Color.Black), 1);
                var gridPen2 = new Pen(Color.FromArgb(50, Color.Black), 25);
                //int cellSize = 256;
                //int boardSize = 7 * cellSize;

                int cellSize = getCellSize();//256;
                int smallCellSize = worldData.BoardLayout.TerrainsPerBoard * cellSize;
                int boardsY = worldData.BoardLayout.BoardSections.Count;
                int boardsX = boardsY > 0 ? worldData.BoardLayout.BoardSections[0].Count : 0;

                for (int boardY = 0; boardY < boardsY; boardY++)
                {
                    for (int boardX = 0; boardX < boardsX; boardX++)
                    {
                        float y = boardX * smallCellSize; //x
                        float x = boardY * smallCellSize; //y
                        //x<->y for vertical maps

                    // Рамка большого квадрата
                    //g.DrawRectangle(Pens.Black, x, y, boardSize, boardSize);
                    g.DrawRectangle(gridPen2, x, y, smallCellSize, smallCellSize);

                    // Сетка внутри большого квадрата
                    for (int i = 1; i < worldData.BoardLayout.TerrainsPerBoard; i++)
                        {
                            g.DrawLine(gridPen, x + i * cellSize, y, x + i * cellSize, y + smallCellSize);
                            g.DrawLine(gridPen, x, y + i * cellSize, x + smallCellSize, y + i * cellSize);
                        }
                    }
                }
            }

        private void DrawTerrain(Graphics g)
        {
            if (worldData?.BoardLayout?.BoardSections == null) return;

            //int cellSize = 256;
            //int boardsY = worldData.BoardLayout.BoardSections.Count;
            //int boardsX = boardsY > 0 ? worldData.BoardLayout.BoardSections[0].Count : 0;

            // Определяем размеры из данных
            int cellSize = getCellSize();//256;
            int smallCellSize = worldData.BoardLayout.TerrainsPerBoard * cellSize;
            int boardsY = worldData.BoardLayout.BoardSections.Count;
            int boardsX = boardsY > 0 ? worldData.BoardLayout.BoardSections[0].Count : 0;

            for (int boardY = 0; boardY < boardsY; boardY++)
            {
                var boardRow = worldData.BoardLayout.BoardSections[boardY];
                for (int boardX = 0; boardX < boardRow.Count; boardX++)
                {
                    var boardSection = boardRow[boardX];
                    if (boardSection?.Terrains == null) continue;

                    int terrainsY = boardSection.Terrains.Count;
                    int terrainsX = terrainsY > 0 ? boardSection.Terrains[0].Count : 0;

                    // ПОВОРОТ НА 90° НАЛЕВО: меняем X и Y местами и инвертируем
                    float baseX = /*boardX*7*cellSize;*/boardY * smallCellSize;  // boardY становится X
                    float baseY = /*boardY*7*cellSize;*/(boardsX - 1 - boardX) * smallCellSize; // boardX становится Y и инвертируется

                    for (int terrainY = 0; terrainY < terrainsY; terrainY++)
                    {
                        var terrainRow = boardSection.Terrains[terrainY];
                        for (int terrainX = 0; terrainX < terrainsX; terrainX++)
                        {
                            var terrain = terrainRow[terrainX];
                            if (terrain == null) continue;

                            // ИНВЕРСИЯ ВЕРТИКАЛИ внутри большого квадрата
                            //float x = baseX + terrainX * cellSize;
                            //float y = baseY + terrainY * cellSize;//(terrainsY - 1 - terrainY) * cellSize;
                            float x = baseX + terrainY * cellSize;//(terrainsY - 1 - terrainY) * cellSize;
                            float y = baseY + (terrainsX - 1 - terrainX) * cellSize;

                            float height = CalculateHeight(terrain, new PointF(x + cellSize / 2, y + cellSize / 2));

                            Color heightColor = new Color(); //= GetHeightColor(height);
                            if(displaySettings.DisplayMode == DisplayMode.Flat) //Yellow Smart
                                heightColor = GetHeightColor(height);
                            if (displaySettings.DisplayMode == DisplayMode.Ponos) //Ponos original
                                heightColor = GetHeightColorOld(height);

                            using (var brush = new SolidBrush(heightColor))
                            {
                                g.FillRectangle(brush, x, y, cellSize, cellSize);
                                /*var font = new Font("Arial", 72, FontStyle.Bold);
                                var textBrush = new SolidBrush(Color.Black);
                                int ii = terrainX + terrainY * 7;
                                g.DrawString(ii.ToString(), font, textBrush, x + cellSize/ 2, y + cellSize / 2-50);*/
                            }
                            
                            DrawTerrainInfo(g, terrain, x, y, cellSize, height);
                        }
                    }

                    /*var font2 = new Font("Arial", 256, FontStyle.Bold);
                    var textBrush2 = new SolidBrush(Color.Black);
                    float ii2 = boardX + boardY * 3;
                    g.DrawString(ii2.ToString(), font2, textBrush2, baseX + 7 * cellSize / 2, baseY + +7 * cellSize / 2 - 100);*/
                }
            }
        }

        private float CalculateHeight(Terrain terrain, PointF worldPos)
            {
                float baseHeight = (float)terrain.BaseHeight;
                float heightScale = (float)terrain.HeightScale;

                PointF desmartifiedPos = new PointF(worldPos.X / (float)lengthToSmartK(), worldPos.Y / (float)lengthToSmartK());
                
                // Применяем модификаторы от гор
                if (displaySettings.ApplyHills)
                foreach (var mountain in mountains)
                {
                    float distance = Distance0(desmartifiedPos, mountain.Position)* (float)lengthToSmartK();
                    if (distance <= mountain.Radius * (float)lengthToSmartK())
                    {
                        float influence = 1 - ((distance - mountain.InnerRadius * (float)lengthToSmartK()) / (mountain.Radius * (float)lengthToSmartK()));
                        if (distance <= mountain.InnerRadius * (float)lengthToSmartK()) influence = 1.0f;
                        baseHeight += mountain.CenterBaseHeightMod * influence +
                                     mountain.BorderBaseHeightMod * (1 - influence);
                        heightScale += mountain.CenterHeightScaleMod * influence +
                                      mountain.BorderHeightScaleMod * (1 - influence);
                    }
                }

                // Ограничиваем значения
                baseHeight = Math.Max(-1, Math.Min(1, baseHeight));
                heightScale = Math.Max(0, Math.Min(1, heightScale));

                // Вычисляем итоговую высоту
                //if (!displaySettings.ApplyHills)
                //{
                //    return worldData.BoardLayout.WorldHeightAndDepth * baseHeight; // RawHeight
                //    
                //}

                return displaySettings.HeightMode switch
                {
                    HeightMode.Average => worldData.BoardLayout.WorldHeightAndDepth * baseHeight * (1 + 0.3f*heightScale),
                    HeightMode.Max => worldData.BoardLayout.WorldHeightAndDepth * baseHeight * (1 + 0.8f * heightScale), // k1 = 0.8
                    HeightMode.Min => worldData.BoardLayout.WorldHeightAndDepth * baseHeight * (1 - 0.2f * heightScale), // k2 = -0.2
                    HeightMode.Raw => worldData.BoardLayout.WorldHeightAndDepth * baseHeight,
                    HeightMode.Straight => worldData.BoardLayout.WorldHeightAndDepth * baseHeight*(1 + heightScale),
                    _ => worldData.BoardLayout.WorldHeightAndDepth * baseHeight
                };
            }

        private Color GetHeightColor(float height)
        {
            float waterLevel = (float)numWaterLevel.Value;
            float minHeight = -worldData.BoardLayout.WorldHeightAndDepth; //* (1 + 0.2f); // ScaledHeightMin с k2=0.2
            float maxHeight = worldData.BoardLayout.WorldHeightAndDepth * 2;//* (1 + 0.8f);  // ScaledHeightMax с k1=0.8

            // Нормализуем высоту в диапазон [0, 1]
            float normalized = (height - minHeight) / (maxHeight - minHeight);
            normalized = Math.Max(0, Math.Min(1, normalized)); // Ограничиваем

            // Цветовые точки градиента
            Color darkBlue = Color.FromArgb(0, 0, 100);     // -500m
            Color lightBlue = Color.FromArgb(100, 150, 255); // 0m
            Color yellow = Color.FromArgb(255, 255, 0);     // Уровень моря
            Color darkBrown = Color.FromArgb(101, 67, 33);  // +500m

            if (height <= 0)
            {
                // Под водой: от темно-синего до светло-голубого
                float underwaterFactor = height < 0 ? (height - minHeight) / (0 - minHeight) : 1;
                underwaterFactor = Math.Max(0, Math.Min(1, underwaterFactor));
                return InterpolateColor(darkBlue, lightBlue, underwaterFactor);
            }
            else if (height <= waterLevel)
            {
                // От 0 до уровня моря: от светло-голубого до желтого
                float beachFactor = height / waterLevel;
                return InterpolateColor(lightBlue, yellow, beachFactor);
            }
            else
            {
                // Выше уровня моря: от желтого до красно-коричневого
                float landFactor = (height - waterLevel) / (maxHeight - waterLevel);
                landFactor = Math.Max(0, Math.Min(1, landFactor));
                return InterpolateColor(yellow, darkBrown, landFactor);
            }
        }

        private Color GetHeightColorOld(float height)
        {
            float waterLevel = (float)numWaterLevel.Value;

            // Цветовая схема: от синего (низ) через желтый (уровень воды) к коричневому (верх)
            if (height <= waterLevel - 100) return Color.DarkBlue;
            if (height <= waterLevel - 50) return Color.Blue;
            if (height <= waterLevel - 10) return Color.LightBlue;
            if (height <= waterLevel) return Color.Yellow;
            if (height <= waterLevel + 100) return Color.Green;
            if (height <= waterLevel + 300) return Color.GreenYellow;
            if (height <= waterLevel + 500) return Color.Orange;
            return Color.Brown;
        }

        private Color InterpolateColor(Color color1, Color color2, float factor)
        {
            factor = Math.Max(0, Math.Min(1, factor));

            int r = (int)(color1.R + (color2.R - color1.R) * factor);
            int g = (int)(color1.G + (color2.G - color1.G) * factor);
            int b = (int)(color1.B + (color2.B - color1.B) * factor);

            return Color.FromArgb(r, g, b);
        }

        private void DrawTerrainInfo(Graphics g, Terrain terrain, float x, float y, float cellSize, float height)
            {
                using (var font = new Font("Arial", 10, FontStyle.Bold)) // Увеличил шрифт
                using (var textBrush = new SolidBrush(Color.Black))
                using (var backgroundBrush = new SolidBrush(Color.FromArgb(200, Color.White)))
                {
                    // Высота с фоном для читаемости
                    string heightText = $"{height:F0}m";
                    var textSize = g.MeasureString(heightText, font);

                    // Фон для текста
                    g.FillRectangle(backgroundBrush, x + (cellSize - textSize.Width) / 2 - 2,
                                   y + 5 - 2, textSize.Width + 4, textSize.Height + 4);

                    g.DrawString(heightText, font, textBrush,
                        x + (cellSize - textSize.Width) / 2, y + 5);

                    // Биом - увеличенная точка
                    if (BiomeColors.TryGetValue(terrain.Biome, out Color biomeColor))
                    {
                        using (var biomeBrush = new SolidBrush(biomeColor))
                        using (var biomeBorder = new Pen(Color.Black, 1))
                        {
                            float biomeSize = 14; // Увеличил размер
                            g.FillEllipse(biomeBrush, x + 8, y + cellSize - 20, biomeSize, biomeSize);
                            g.DrawEllipse(biomeBorder, x + 8, y + cellSize - 20, biomeSize, biomeSize);

                            // Текст биома
                            string biomeText = terrain.Biome.ToString();
                            var biomeTextSize = g.MeasureString(biomeText, font);
                            g.DrawString(biomeText, font, textBrush,
                                x + 8 + (biomeSize - biomeTextSize.Width) / 2,
                                y + cellSize - 20 + (biomeSize - biomeTextSize.Height) / 2);
                        }
                    }

                    // Точки холмистости - более заметные
                    if (terrain.HeightScale > 0)
                    {
                        //float dotSize = (float)(5 + terrain.HeightScale * 12); // Увеличил базовый размер
                        float dotSize = (float)(5 + terrain.HeightScale * cellSize/4);
                        float strangeOffsetCompensation = -dotSize / 2; //-cellSize / 4 / 2;
                        float otstupFromCenter = cellSize / 4;
                        float otstupToCenter = cellSize / 2 + strangeOffsetCompensation;
                        using (var dotBrush = new SolidBrush(Color.FromArgb(200, Color.DarkGray)))
                        {
                            // Основная точка справа сверху
                            //g.FillEllipse(dotBrush, x + cellSize - 25, y + 8, dotSize, dotSize);

                            // Дополнительные точки
                            //g.FillEllipse(dotBrush, x + 10, y + 25, dotSize / 1.5f, dotSize / 1.5f);
                            //g.FillEllipse(dotBrush, x + cellSize - 20, y + cellSize - 25, dotSize / 1.5f, dotSize / 1.5f);

                            //4 точки в центре
                            g.FillEllipse(dotBrush, x + otstupToCenter - otstupFromCenter, y + otstupToCenter - otstupFromCenter, dotSize, dotSize);
                            g.FillEllipse(dotBrush, x + otstupToCenter - otstupFromCenter, y + otstupToCenter + otstupFromCenter, dotSize, dotSize);
                            g.FillEllipse(dotBrush, x + otstupToCenter + otstupFromCenter, y + otstupToCenter + otstupFromCenter, dotSize, dotSize);
                            g.FillEllipse(dotBrush, x + otstupToCenter + otstupFromCenter, y + otstupToCenter - otstupFromCenter, dotSize, dotSize);
                        }
                    }

                    //tile center
                    SolidBrush dotBrush2 = new SolidBrush(Color.FromArgb(200, Color.White));
                    g.FillEllipse(dotBrush2, x + cellSize / 2 - 5 , y + cellSize / 2 - 5 , 10, 10 );
                       
                    
                }
            }

        private void DrawMountains(Graphics g)
            {
                foreach (var mountain in mountains)
                {
                    var screenPos = WorldToScreen2(mountain.Position);
                    screenPos.X *= 1f;//(float)lengthToSmartK();
                    screenPos.Y *= 1f;//(float)lengthToSmartK();
                    float screenRadius = mountain.Radius * (float)lengthToSmartK();//* scale;
                    float screenInnerRadius = mountain.InnerRadius * (float)lengthToSmartK();

                    // Круг влияния
                    using (var pen = new Pen(selectedMountain == mountain ? Color.Red : Color.Black, 2))
                    {
                        pen.DashStyle = DashStyle.Dash;
                        g.DrawEllipse(pen, screenPos.X - screenRadius, screenPos.Y - screenRadius,
                                     screenRadius * 2, screenRadius * 2);

                    }
                    // Внутренний круг
                    using (var pen = new Pen(selectedMountain == mountain ? Color.Red : Color.Black, 2))
                    {
                        pen.DashStyle = DashStyle.Dot;
                        g.DrawEllipse(pen, screenPos.X - screenInnerRadius, screenPos.Y - screenInnerRadius,
                                     screenInnerRadius * 2, screenInnerRadius * 2);

                    }
                    // Центр горы
                    using (var brush = new SolidBrush(selectedMountain == mountain ? Color.Red : Color.DarkGray))
                    {
                        g.FillEllipse(brush, screenPos.X - 5, screenPos.Y - 5, 10, 10);
                    }
                }
            }

            private void DrawSelections(Graphics g)
            {
                // Здесь будет рисование выделенных элементов

                if(selectedTerrain!=null)
                {
                    float[] CoordPair = getCoordsByTerrain(selectedTerrain);
                    //if (CoordPair == null) Break;
                    var SelectionPen = new Pen(Color.FromArgb(95, Color.White), 50);
                    g.DrawRectangle(SelectionPen, CoordPair[0]-5, CoordPair[1]-5, getCellSize() + 5, getCellSize() + 5);

                }
            }

        private float[] getCoordsByTerrain(Terrain t)
        {
            float[] ans =  new float[2];
            ans[0] = 0;
            ans[1] = 0;

            if (t == null) return null;
            if (worldData?.BoardLayout?.BoardSections == null) return null;

            //int cellSize = 256;
            int cellSize = getCellSize();//256;
            int smallCellSize = worldData.BoardLayout.TerrainsPerBoard * cellSize;
            int boardsY = worldData.BoardLayout.BoardSections.Count;
            int boardsX = boardsY > 0 ? worldData.BoardLayout.BoardSections[0].Count : 0;

            for (int boardY = 0; boardY < boardsY; boardY++)
            {
                var boardRow = worldData.BoardLayout.BoardSections[boardY];
                for (int boardX = 0; boardX < boardRow.Count; boardX++)
                {
                    var boardSection = boardRow[boardX];
                    if (boardSection?.Terrains == null) continue;

                    int terrainsY = boardSection.Terrains.Count;
                    int terrainsX = terrainsY > 0 ? boardSection.Terrains[0].Count : 0;

                    // ТА ЖЕ ТРАНСФОРМАЦИЯ ЧТО И В DrawTerrain
                    float baseX = boardY * smallCellSize;
                    float baseY = (boardsX - 1 - boardX) * smallCellSize;

                    for (int terrainY = 0; terrainY < terrainsY; terrainY++)
                    {
                        var terrainRow = boardSection.Terrains[terrainY];
                        for (int terrainX = 0; terrainX < terrainsX; terrainX++)
                        {
                            // ТА ЖЕ ТРАНСФОРМАЦИЯ ЧТО И В DrawTerrain
                            float x = baseX + terrainY * cellSize;
                            float y = baseY + (terrainsX - 1 - terrainX) * cellSize;

                            if (terrainRow[terrainX]==t)
                            {
                                ans[0] = x;
                                ans[1] = y;
                                return ans;
                            }

                        }
                    }
                }
            }

            return null;
            //return ans;
        }

        private void FitToView()
        {
            if (worldData?.BoardLayout?.BoardSections == null) return;

            int boardsY = worldData.BoardLayout.BoardSections.Count;
            int boardsX = boardsY > 0 ? worldData.BoardLayout.BoardSections[0].Count : 0;

            if (boardsX == 0 || boardsY == 0) return;

            int cellSize = getCellSize();//256;
            int miniGridSize = worldData.BoardLayout.TerrainsPerBoard;

            // ВАЖНО: учитываем трансформацию координат (поворот на 90° налево)
            // После поворота: общая ширина = исходная высота, общая высота = исходная ширина
            float totalWidth = boardsY * miniGridSize * cellSize;   // boardY становится X
            float totalHeight = boardsX * miniGridSize * cellSize;  // boardX становится Y

            // Вычисляем доступную область для рисования (исключаем панели)
            int availableWidth = this.ClientSize.Width - (propertiesPanel?.Width ?? 300);
            int availableHeight = this.ClientSize.Height - (toolPanel?.Height ?? 40);

            // Вычисляем масштаб чтобы вместить всю карту
            float scaleX = availableWidth / totalWidth;
            float scaleY = availableHeight / totalHeight;

            scale = Math.Min(scaleX, scaleY) * 0.95f; // 95% от максимального размера
            //scale = Math.Max(0.1f, Math.Min(2.0f*(float)worldData.BoardLayout.TerrainSize/256.0f, scale)); // Ограничения

            // ЦЕНТРИРУЕМ КАРТУ - теперь правильно учитываем панели
            panOffset.X = (availableWidth - totalWidth * scale) / 2;
            panOffset.Y = (availableHeight - totalHeight * scale) / 2;

            // Добавляем смещение для панели инструментов
            if (toolPanel != null)
                panOffset.Y += toolPanel.Height;

            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            FitToView();
        }

        // Обработчики мыши
        private void MainForm_MouseWheel(object sender, MouseEventArgs e)
            {
                var worldPosBefore = ScreenToWorld(e.Location);

                float zoomFactor = 1.1f;
                if (e.Delta > 0)
                    scale *= zoomFactor;
                else
                    scale /= zoomFactor;

                //scale = Math.Max(0.1f, Math.Min(5.0f, scale));

                var worldPosAfter = ScreenToWorld(e.Location);
                panOffset.X += (worldPosAfter.X - worldPosBefore.X) * scale;
                panOffset.Y += (worldPosAfter.Y - worldPosBefore.Y) * scale;

                Invalidate();
            }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            lastMousePos = e.Location;
            var worldPos = ScreenToWorld(e.Location);
            

            if (e.Button == MouseButtons.Right)
            {
                isPanning = true;
                this.Cursor = Cursors.SizeAll;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    if (currentMode == EditorMode.Brush)
                    {
                        // Режим кисти - применяем кисть
                        ApplyBrush(worldPos);
                        return;
                    }
                    else
                    {
                        // Режим выбора или горы - выбираем объект
                        if (currentMode == EditorMode.Select)
                        {
                            SelectTerrainOrMountain(worldPos);
                            return;
                        }

                        if (currentMode == EditorMode.Mountain /*&& selectedMountain == null*/)
                        {
                            PointF desmartifiedPos = new PointF(worldPos.X / (float)lengthToSmartK(), worldPos.Y / (float)lengthToSmartK());
                            // Если в режиме горы и ничего не выбрано - создаем новую гору
                            CreateMountain(desmartifiedPos);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MouseDown error: {ex.Message}");
                }
            }
        }

        private bool IsPointNearMountain(PointF point, Mountain mountain)
        {
            return Distance(point, mountain.Position) < 20; // 20 пикселей для захвата
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning && e.Button == MouseButtons.Right)
            {
                panOffset.X += e.X - lastMousePos.X;
                panOffset.Y += e.Y - lastMousePos.Y;
                lastMousePos = e.Location;
                Invalidate();
            }
            else if (isDraggingMountain && selectedMountain != null)
            {
                var worldPos = ScreenToWorld(e.Location);
                selectedMountain.Position = worldPos;
                UpdateMountainProperties();
                Invalidate();
            }
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
            isDraggingMountain = false;
            this.Cursor = Cursors.Default;
        }

        // Вспомогательные методы преобразования координат
        private PointF ScreenToWorld(Point screenPoint)
            {
                return new PointF(
                    (screenPoint.X - panOffset.X) / scale /* (float)lengthToSmartK()*/,
                    (screenPoint.Y - panOffset.Y) / scale /* (float)lengthToSmartK()*/
                );
            }

            private Point WorldToScreen(PointF worldPoint)
            {
                return new Point(
                    //(int)(worldPoint.X * scale + panOffset.X), //WORLD=(SCREEN-panOffset)/scale ->scale*world+panoffset
                    //(int)(worldPoint.Y * scale + panOffset.Y)
                    (int)(worldPoint.X  ),
                    (int)(worldPoint.Y  )
                );
            }

            private PointF WorldToScreen2(PointF worldPoint)
            {
                return new PointF(
                    //(int)(worldPoint.X * scale + panOffset.X), //WORLD=(SCREEN-panOffset)/scale ->scale*world+panoffset
                    //(int)(worldPoint.Y * scale + panOffset.Y)
                    worldPoint.X *(float)lengthToSmartK(),
                    worldPoint.Y *(float)lengthToSmartK()
                );
            }

        private float Distance(PointF a, PointF b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy)* (float)lengthToSmartK();
            }
        private float Distance0(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private Mountain? getMountainForeach(PointF worldPos)
        {
            PointF worldPosInSizedCoords = new PointF(worldPos.X / (float)lengthToSmartK(), worldPos.Y / (float)lengthToSmartK());
            foreach (Mountain m in mountains)
            {
                if(Distance0(worldPosInSizedCoords, m.Position) < (20 / (float)lengthToSmartK()))
                {
                    return m;
                }
            }
            return null;
        }

        // Методы для работы с данными
        private void SelectTerrainOrMountain(PointF worldPos)
        {
            // Сначала проверяем горы
            PointF worldPosInSizedCoords = new PointF(worldPos.X / (float)lengthToSmartK(), worldPos.Y / (float)lengthToSmartK());
            selectedMountain = mountains.LastOrDefault(m => Distance0(worldPosInSizedCoords, m.Position) < (20 / (float)lengthToSmartK())); //FSR it has problems with vertical maps (below the upper square it cannot catch !=null)
            //this.Text=("ABOBA " + worldPosInSizedCoords + "<-Coord CLICK" + Distance0(worldPosInSizedCoords, selectedMountain.Position) + "<" + (20 / (float)lengthToSmartK()) + " =" + (Distance0(worldPosInSizedCoords, selectedMountain.Position) < (20 / (float)lengthToSmartK())));
            //"System.ArgumentOutOfRangeException"
            //selectedMountain = getMountainForeach(worldPos);
            //this.Text=("ABOBA " + worldPosInSizedCoords + "<-Coord CLICK" + Distance0(worldPosInSizedCoords, selectedMountain.Position) + "<" + (20 / (float)lengthToSmartK()) + " =" + (Distance0(worldPosInSizedCoords, selectedMountain.Position) < (20 / (float)lengthToSmartK())));
            if (selectedMountain != null)
            {
                selectedTerrain = null;
                UpdateMountainProperties();

                // Показываем панель свойств горы
                if (terrainPropertiesGroup != null) terrainPropertiesGroup.Visible = false;
                if (mountainPropertiesGroup != null) mountainPropertiesGroup.Visible = true;
                
                Invalidate();
                return;
            }
            else
            {
                // Ничего не выбрано - скрываем панели
                //if (terrainPropertiesGroup != null) terrainPropertiesGroup.Visible = false;
                if (mountainPropertiesGroup != null) mountainPropertiesGroup.Visible = false;
            }
            // Затем террейны
            selectedTerrain = FindTerrainAtPosition(worldPos);
            if (selectedTerrain != null)
            {
                selectedMountain = null;
                UpdateTerrainProperties();

                // Показываем панель свойств террейна
                if (terrainPropertiesGroup != null) terrainPropertiesGroup.Visible = true;
                if (mountainPropertiesGroup != null) mountainPropertiesGroup.Visible = false;

                Invalidate();
                return;
            }
            else
            {
                // Ничего не выбрано - скрываем панели
                if (terrainPropertiesGroup != null) terrainPropertiesGroup.Visible = false;
                //if (mountainPropertiesGroup != null) mountainPropertiesGroup.Visible = false;
            }
        }

        private Terrain FindTerrainAtPosition(PointF worldPos)
        {
            if (worldData?.BoardLayout?.BoardSections == null) return null;

            int cellSize = getCellSize();//256;
            int smallCellSize = worldData.BoardLayout.TerrainsPerBoard * cellSize;
            int boardsY = worldData.BoardLayout.BoardSections.Count;
            int boardsX = boardsY > 0 ? worldData.BoardLayout.BoardSections[0].Count : 0;

            for (int boardY = 0; boardY < boardsY; boardY++)
            {
                var boardRow = worldData.BoardLayout.BoardSections[boardY];
                for (int boardX = 0; boardX < boardRow.Count; boardX++)
                {
                    var boardSection = boardRow[boardX];
                    if (boardSection?.Terrains == null) continue;

                    int terrainsY = boardSection.Terrains.Count;
                    int terrainsX = terrainsY > 0 ? boardSection.Terrains[0].Count : 0;

                    // ТА ЖЕ ТРАНСФОРМАЦИЯ ЧТО И В DrawTerrain
                    float baseX = boardY * smallCellSize;
                    float baseY = (boardsX - 1 - boardX) * smallCellSize;

                    for (int terrainY = 0; terrainY < terrainsY; terrainY++)
                    {
                        var terrainRow = boardSection.Terrains[terrainY];
                        for (int terrainX = 0; terrainX < terrainsX; terrainX++)
                        {
                            // ТА ЖЕ ТРАНСФОРМАЦИЯ ЧТО И В DrawTerrain
                            float x = baseX + terrainY * cellSize;
                            float y = baseY + (terrainsX - 1 - terrainX) * cellSize;

                            if (worldPos.X >= x && worldPos.X <= x + cellSize &&
                                worldPos.Y >= y && worldPos.Y <= y + cellSize)
                            {
                                return terrainRow[terrainX];
                            }
                        }
                    }
                }
            }

            return null;
        }

            //applies BaseHeight,HeightScale,Biome of storeTerrain
            private void ApplyBrush(PointF worldPos)
            {
                //System.Console.Out.WriteLine("YES!");
                var targetTerrain = FindTerrainAtPosition(worldPos);
                if (targetTerrain != null && storeTerrain != null)
                {
                    SaveUndoState("Apply Brush");

                    // Применяем характеристики выбранного террейна к целевому
                    targetTerrain.BaseHeight = storeTerrain.BaseHeight;
                    targetTerrain.HeightScale = storeTerrain.HeightScale;
                    targetTerrain.Biome = storeTerrain.Biome;

                    Invalidate();
                }
            }

            private void CreateMountain(PointF worldPos)
            {
                if (storeMountain==null)
                    storeMountain = new Mountain
                    {
                        Position = worldPos,
                        Radius = 500,
                        CenterBaseHeightMod = 0.1f,
                        BorderBaseHeightMod = 0,
                        CenterHeightScaleMod = 0.2f,
                        BorderHeightScaleMod = 0
                    };
                storeMountain.Position = worldPos;
                Mountain tempMountain = new Mountain { };
                tempMountain.copyDataFrom(storeMountain);
                mountains.Add(tempMountain);
                //selectedMountain = storeMountain;

                UpdateMountainProperties();
                Invalidate();
            }

            private void DeleteSelectedMountain(object sender, EventArgs e)
            {
                if(selectedMountain!=null)
                {
                    mountains.Remove(selectedMountain);
                    selectedMountain = null;
                }
                UpdateMountainProperties();
                Invalidate();
            }

            private void UpdateTerrainProperties()
            {
                //запись СТ в панель (приоритетнее). Если она будет стоять снизу, то после кисти при выборе террейна в него кисть запишется, лол
                if (selectedTerrain != null)
                {
                    numBaseHeight.Value = (decimal)selectedTerrain.BaseHeight;
                    numHeightScale.Value = (decimal)selectedTerrain.HeightScale;
                    numBiome.Value = selectedTerrain.Biome;

                    // Обновляем метры
                    UpdateMeterValues();
                }

                //запись буфера кисти в панель
                if (storeTerrain != null)
                {
                    numBaseHeight.Value = (decimal)storeTerrain.BaseHeight;
                    numHeightScale.Value = (decimal)storeTerrain.HeightScale;
                    numBiome.Value = storeTerrain.Biome;

                    // Обновляем метры
                    UpdateMeterValues();
                }
                
            }

            private void UpdateMountainProperties()
            {
                if (storeMountain != null)
                {
                    numMountainX.Value = (decimal)storeMountain.Position.X;
                    numMountainY.Value = (decimal)storeMountain.Position.Y;
                    numMountainRadius.Value = (decimal)storeMountain.Radius;
                    numCenterBaseMod.Value = (decimal)storeMountain.CenterBaseHeightMod;
                    numBorderBaseMod.Value = (decimal)storeMountain.BorderBaseHeightMod;
                    numCenterScaleMod.Value = (decimal)storeMountain.CenterHeightScaleMod;
                    numBorderScaleMod.Value = (decimal)storeMountain.BorderHeightScaleMod;
                }

                //у гор внезапно нет таких проблем с записью, как у террейна. Обожаю произведение своей смартности на смартный код дипсратя
                if (selectedMountain != null)
                {
                    numMountainX.Value = (decimal)selectedMountain.Position.X;
                    numMountainY.Value = (decimal)selectedMountain.Position.Y;
                    numMountainRadius.Value = (decimal)selectedMountain.Radius;
                    numCenterBaseMod.Value = (decimal)selectedMountain.CenterBaseHeightMod;
                    numBorderBaseMod.Value = (decimal)selectedMountain.BorderBaseHeightMod;
                    numCenterScaleMod.Value = (decimal)selectedMountain.CenterHeightScaleMod;
                    numBorderScaleMod.Value = (decimal)selectedMountain.BorderHeightScaleMod;
                }
            }

            private void UpdateMeterValues() //probably incorrect formulas, recheck
            {
                float baseHeightMeters = (float)(worldData.BoardLayout.WorldHeightAndDepth * numBaseHeight.Value);
                float heightScaleMeters = baseHeightMeters * (float)( numHeightScale.Value);

                lblBaseHeightMeters.Text = $"{baseHeightMeters:F1}m";
                lblHeightScaleMeters.Text = $"{heightScaleMeters:F1}m";
                lblRawHeight.Text = $"{worldData.BoardLayout.WorldHeightAndDepth * numBaseHeight.Value:F1}m";
            }

            private void SaveUndoState(string actionName)
            {
                // Реализация системы отмены
                var action = new TerrainAction
                {
                    Name = actionName,
                    // Сохраняем состояние мира
                };
                undoStack.Push(action);
                redoStack.Clear();
                UpdateUndoRedoButtons();
            }

            private void UpdateUndoRedoButtons()
            {
                btnUndo.Enabled = undoStack.Count > 0;
                btnRedo.Enabled = redoStack.Count > 0;
            }

            // Обработчики кнопок
            private void BtnNew_Click(object sender, EventArgs e)
            {
                worldData = new WorldData();
                mountains.Clear();
                InitializeWorldData(); //temporary...
                wipeUnnecData();
                Invalidate();
            }

            private async void BtnOpen_Click(object sender, EventArgs e)
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "World files (*.world)|*.world|JSON files (*.json)|*.json|All files (*.*)|*.*";
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        UpdateToolButtons();
                        try
                        {
                            string json = await File.ReadAllTextAsync(dialog.FileName);
                            worldData = JsonSerializer.Deserialize<WorldData>(json);
                            InitializeWorldData(); // Дополнительная инициализация
                            wipeUnnecData();
                            LoadMountainsFromWorldData();
                            FitToView(); // Подгоняем под вид после загрузки
                            Invalidate();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

        private async void BtnSave_Click(object sender, EventArgs e)
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "World files (*.world)|*.world|JSON files (*.json)|*.json";
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            SaveMountainsToWorldData();
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            string json = JsonSerializer.Serialize(worldData, options);
                            await File.WriteAllTextAsync(dialog.FileName, json);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            private void BtnUndo_Click(object sender, EventArgs e) { /* Реализация отмены */ }
            private void BtnRedo_Click(object sender, EventArgs e) { /* Реализация повтора */ }

            private void SettingsChanged(object sender, EventArgs e)
            {
                displaySettings.ApplyHills = chkApplyHills.Checked;
                displaySettings.HeightMode = (HeightMode)cmbHeightMode.SelectedIndex;
                displaySettings.DisplayMode = (DisplayMode)cmbDisplayMode.SelectedIndex;
                Invalidate();
            }

            private void LoadMountainsFromWorldData()
            {
                // Загрузка гор из worldData
                mountains.Clear();
            }

            private void SaveMountainsToWorldData()
            {
                // Сохранение гор в worldData
            }

            private void UpdateToolButtons()
            {
                // Сбрасываем выделение при смене инструмента
                //selectedTerrain = null;
                selectedMountain = null;
                updateNUDBoundaries();
                if (currentMode==EditorMode.Brush)
                {
                    if (selectedTerrain == null)
                        selectedTerrain = new Terrain()
                        {
                            BaseHeight = 0f,
                            HeightScale = 0f,
                            Biome = 3,
                            Seed = 1, //seed 1 is a bad thing, need a generator
                            PerlinFrequency = 4,
                            PerlinOctaves = 5
                        };
                    storeTerrain = new Terrain();
                    storeTerrain.copyDataFrom(selectedTerrain);
                    selectedTerrain = null;
                    //terrainPropertiesGroup.Visible = true;
                }


                if(currentMode==EditorMode.Mountain)
                {
                    if (selectedMountain == null)
                    {
                        selectedMountain = new Mountain
                        {
                            Position = new PointF(0, 0),
                            Radius = 500,
                            CenterBaseHeightMod = 0.1f,
                            BorderBaseHeightMod = 0,
                            CenterHeightScaleMod = 0.2f,
                            BorderHeightScaleMod = 0
                        };
                    }
                    storeMountain = new Mountain();
                    storeMountain.copyDataFrom(selectedMountain);
                    selectedMountain = null;
                }

                bool showTerr = currentMode == EditorMode.Brush || (currentMode == EditorMode.Select && selectedTerrain != null);
                bool showMount = currentMode == EditorMode.Mountain || (currentMode == EditorMode.Select && selectedMountain != null);

                // Обновляем видимость панелей свойств
                if (terrainPropertiesGroup != null && showTerr)
                    terrainPropertiesGroup.Visible = true;
                if (mountainPropertiesGroup != null && showMount)
                    mountainPropertiesGroup.Visible = true;
            


                if (lblEditorModeVal != null)
                {
                    lblEditorModeVal.Text = $"{getEditorText():F1}";
                }
                if (lblMountEditorModeVal != null)
                {
                    lblMountEditorModeVal.Text = $"{getMountEditorText():F1}";
                }
                Invalidate();
            }

            private void updateNUDBoundaries()
            {
                if (numMountainX != null)
                    numMountainX.Maximum = (decimal)(worldData.BoardLayout.BoardSections.Count * worldData.BoardLayout.TerrainsPerBoard * worldData.BoardLayout.TerrainSize);
                if (numMountainY != null)
                    numMountainY.Maximum = (decimal)(worldData.BoardLayout.BoardSections.Count * worldData.BoardLayout.TerrainsPerBoard * worldData.BoardLayout.TerrainSize);
                if (numMountainRadius != null)
                    numMountainRadius.Maximum = (decimal)(worldData.BoardLayout.BoardSections.Count * worldData.BoardLayout.TerrainsPerBoard * worldData.BoardLayout.TerrainSize / 2);
                if (numMountainInnerRadius != null) //This code is for initialisation for case where the upper NUP is null for some reason (this should not happen)
                    numMountainInnerRadius.Maximum = (decimal)(worldData.BoardLayout.BoardSections.Count * worldData.BoardLayout.TerrainsPerBoard * worldData.BoardLayout.TerrainSize / 2);
                if (numMountainInnerRadius != null && numMountainRadius!=null) //this is updated in numMountainRadius change. This code is for initialisation
                    numMountainInnerRadius.Maximum = numMountainRadius.Value;//(decimal)(worldData.BoardLayout.BoardSections.Count * worldData.BoardLayout.TerrainsPerBoard * worldData.BoardLayout.TerrainSize / 2);
            }

            private int getCellSize()
            {
                int ans = 1;
                
                if(displaySettings.UseTrueSize)
                {
                    ans= (int)worldData.BoardLayout.TerrainSize;
                }
                else
                {
                    ans = 256;
                }

                return ans;
            }
            
            //smartLength=actualLength*K, k=1/terrainSize, SizedLenght=256lng/k
            private double lengthToSmartK()
            {
                double ans = 1.0f;
                if (displaySettings.UseTrueSize_SmartifyLenghts && worldData.BoardLayout!=null)
                {
                    ans = 256/worldData.BoardLayout.TerrainSize;
                }
                return ans;
            }

            private double lengthToSmartK2()
            {
                double ans = 1.0f;
                if (displaySettings.UseTrueSize_SmartifyLenghts && worldData.BoardLayout != null)
                {
                    ans = 256 * worldData.BoardLayout.TerrainsPerBoard / worldData.BoardLayout.TerrainSize;
                }
                return ans;
            }

        public void wipeUnnecData()
            {
                selectedTerrain = null;
                selectedMountain = null;
            }
    }

        // Вспомогательные классы
        public enum EditorMode { Select, Brush, Mountain }
        public enum HeightMode { Average, Max, Min, Raw, Straight }
        public enum DisplayMode { Flat, Gradient, Ponos }

        public class DisplaySettings
        {
            public bool ApplyHills { get; set; } = true;
            public HeightMode HeightMode { get; set; } = HeightMode.Average;
            public DisplayMode DisplayMode { get; set; } = DisplayMode.Flat;
            public bool UseTrueSize { get; set; } = false; //if terrain size >256 and you want 1:1 scales with coords
            public bool UseTrueSize_SmartifyLenghts { get; set; } = false; //turn true to have 1:1 coords and scales while UseTrueSize is false
        }

        public class TerrainBrush
        {
            public int Size { get; set; } = 1;
            public float BaseHeight { get; set; }
            public float HeightScale { get; set; }
            public int Biome { get; set; }
        }

        public class Mountain
        {
            public PointF Position { get; set; }
            public float Radius { get; set; } = 500;
            public float InnerRadius { get; set; } = 0;
            public float CenterBaseHeightMod { get; set; }
            public float BorderBaseHeightMod { get; set; }
            public float CenterHeightScaleMod { get; set; }
            public float BorderHeightScaleMod { get; set; }

            public void copyDataFrom(Mountain t)
            {
                this.Position = t.Position;
                this.Radius = t.Radius;
                this.InnerRadius = t.InnerRadius;
                this.CenterBaseHeightMod = t.CenterBaseHeightMod;
                this.BorderBaseHeightMod = t.BorderBaseHeightMod;
                this.CenterHeightScaleMod = t.CenterHeightScaleMod;
                this.BorderHeightScaleMod = t.BorderHeightScaleMod;

            }
        }

        public class TerrainAction
        {
            public string Name { get; set; }
            // Дополнительные данные для отмены
        }
    }

