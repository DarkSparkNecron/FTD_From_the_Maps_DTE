using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTDMapgen_WinForms
{
    partial class MainForm
    {
        //go to MapGenerator in ftd.maps.generation in ftd.dll for reference of ..everything
        public int MapConstants_HeightMapResolution = 1025; //so this thing gives a size for terrainHeight array, but it is const and we have terrainresolution in the map. You will have outOfBoundEx
        public int MapConstants_SplatMapResolution = 1024;

        public void SMARTFUL_GenerateMap()
        {
            
            //this.PrepForFullChange();
            //List<Task> list = new List<Task>();
            for (int i = 0; i < worldData.BoardLayout.BoardSections.Capacity* worldData.BoardLayout.TerrainsPerBoard/*WorldSpecification.i.BoardLayout.EastWestTerrainCount*/; i++)
            {
                for (int j = 0; j < worldData.BoardLayout.BoardSections[0].Capacity * worldData.BoardLayout.TerrainsPerBoard/*WorldSpecification.i.BoardLayout.NorthSouthTerrainCount*/; j++)
                {
                    int xx = i;
                    int yy = j;
                    //list.Add(new Task(delegate ()
                    //{
                        this.SMARTFUL_GenerateHeightMapForTerrain(xx, yy, true); //x is supposed to be left->right, y up->down. but taking his system i need to check if it is rotated into usable state
                    //checking it is quite easy. just need to draww numbers on grid again in the same for pattern as here
                    //}));
                }
            }
            //TaskThreadPool taskThreadPool = new TaskThreadPool(list, true);
            //this.MarkAlphaMapGridlines(); //white lines on the strat map probably
            //this.FlushChangesHeight(true);
            //this.FlushChangeSplat();
        }

        //IT USES GLOBAL mini-grid COORDS. Insteas of void you can return heighmap poinst and alphaData
        //heightMap i believe is THE map, on futher steps it gets "Dilate"d 2 times. Alpha map is splats
        public void SMARTFUL_GenerateHeightMapForTerrain(int xTerrainCoord, int yTerrainCoord, bool updateSplatsToo)
        {
            //do i need to rotate those maps?...
            float[,] this_heightMap; //a property of MapGenerator. Btw i again forgot how properties and field are separated lol. They are basically the same, substract data complexity
            float[,,] this_alphaMaps;

            //PREP FOR FULL CHANGE PRIVATE METHOD
            bool this_preppedHeight = false;//actually false by default
            bool this_preppedSplat = false;//actually false by default


            bool flag = !this_preppedHeight;
            if (flag || true) //|| truue because it cant stop screaming that i didnt initialise it
            {
                this_heightMap = new float[MapConstants_HeightMapResolution, MapConstants_HeightMapResolution]; //!!! THIS IS THE [,] of HEIGHT DOTS WITHIN TERRAIN !!!
                //initialisation because smart cant
                for(int i=0;i< MapConstants_HeightMapResolution;i++)
                {
                    for (int j = 0; j < MapConstants_HeightMapResolution; j++)
                        this_heightMap[i, j] = 0;
                }
                this_preppedHeight = true;
            }
            bool flag2 = !this_preppedSplat;
            if (flag2 || true) //again for the same reasons. will be filled if updateSplatsToo==true
            {
                this_alphaMaps = new float[MapConstants_SplatMapResolution, MapConstants_SplatMapResolution, 5];
                //initialisation because smart cant
                for (int i = 0; i < MapConstants_HeightMapResolution; i++)
                {
                    for (int j = 0; j < MapConstants_HeightMapResolution; j++)
                        for(int k=0;k<5;k++)
                            this_alphaMaps[i, j,k] = 0;
                }
                this_preppedSplat = true;
            }
            //=====================================

            bool changesFlushedHeight = false; //this
            bool changesFlushedSplat = false; //this
            int heightMapResolutionPerTerrain = SMARTFUL_CONST_HeightMapResolutionPerTerrain();//MapConstants.HeightMapResolutionPerTerrain;
            int num = SMARTFUL_CONST_MapConstants_MarginEastWest() + xTerrainCoord * heightMapResolutionPerTerrain;
            int num2 = SMARTFUL_CONST_MapConstants_MarginNorthSouth() + yTerrainCoord * heightMapResolutionPerTerrain;
            Point worldTerrainCoord = new Point(xTerrainCoord, yTerrainCoord); //instead Point here were a nasty "Point 2D" class that supported coord translation whyever
            //BoardAndTerrainCoord boardAndTerrainCoord = worldTerrainCoord.ToBoardAndTerrain_FromAZeroReference(); //skip it

            //MapTerrainInfo terrainInfo = boardAndTerrainCoord.TerrainInfo;
            Terrain terrainInfo = FindTerrainAtPosition(new Point(worldTerrainCoord.X*256, worldTerrainCoord.Y*256)); //transforms mini-grid to 256x grid (main FtM grid)

            //enumBiome biome = terrainInfo.Biome;
            int biome = terrainInfo.Biome;

            float[,] array = new float[heightMapResolutionPerTerrain, heightMapResolutionPerTerrain];
            new PerlinNoise(terrainInfo.PerlinFrequency, 1f, Mathf_Limited.Min(terrainInfo.PerlinOctaves, 4), terrainInfo.Seed).Run2D(array);
            float num3 = 1f / (float)heightMapResolutionPerTerrain * (float)worldData.BoardLayout.HeightMapResolution;//WorldSpecification.i.BoardLayout.HeightMapResolution;
            for (int i = 0; i < heightMapResolutionPerTerrain; i++)
            {
                for (int j = 0; j < heightMapResolutionPerTerrain; j++)
                {
                    int num4 = num + i; //splatY
                    int num5 = num2 + j; //splatX
                    //MAIN HEIGHT THING!!!!!!!
                    float num6 = SMARTFUL_TERRAININFO_ApplyEdgeEffect(terrainInfo,array[j, i], (int)((float)j * num3), (int)((float)i * num3));
                    this_heightMap[num5, num4] = num6;
                    if (updateSplatsToo) //what is Spalts?
                    {
                        SMARTFUL_HeightAndBiomeToAlpha(num5, num4, num6, biome, this_alphaMaps); //returns 5 alpha layers for each 4 groud texture over seabed one this_alphaMaps[,,0]. All 5 are apllied at the same time, at any time seabed is drawn and 1 layer over, as all others are 0
                    }
                }
            }
        }

        

        public int SMARTFUL_CONST_HeightMapResolutionPerTerrain()
        {
            int terrainsEast = worldData.BoardLayout.BoardSections.Capacity * worldData.BoardLayout.TerrainsPerBoard;
            int terrainsSouth = worldData.BoardLayout.BoardSections[0].Capacity * worldData.BoardLayout.TerrainsPerBoard;
            int Mathf_Max= (terrainsEast > terrainsSouth) ? terrainsEast : terrainsSouth;
            float intermediate = (float)((MapConstants_HeightMapResolution - 1) / Mathf_Max);
            return (int)Math.Floor((double)intermediate);//Mathf.FloorToInt();
        }

        public int SMARTFUL_CONST_MapConstants_MarginEastWest()
        {
            int terrainsEast = worldData.BoardLayout.BoardSections.Capacity * worldData.BoardLayout.TerrainsPerBoard;
            return (MapConstants_HeightMapResolution - SMARTFUL_CONST_HeightMapResolutionPerTerrain() * terrainsEast) / 2;
        }


        public int SMARTFUL_CONST_MapConstants_MarginNorthSouth()
        {
            int terrainsSouth = worldData.BoardLayout.BoardSections[0].Capacity * worldData.BoardLayout.TerrainsPerBoard;
            return (MapConstants_HeightMapResolution - SMARTFUL_CONST_HeightMapResolutionPerTerrain() * terrainsSouth) / 2;
        }


        //this is sort of main method for height
        //due to obviously not having this in the TerrainInfo class, i added new arg for equivalence of functionality
        //there is many references to terrain data, which is double in FtM, but in FtD is float
        //for purpuses of base height and height scale i use their mountain-modified versions
        public float SMARTFUL_TERRAININFO_ApplyEdgeEffect(Terrain THUS,float inputValue, int terrainX, int terrainY)
        {
            int WorldSpecification_i_BoardLayout_HeightMapResolution = 257;
            WorldSpecification_i_BoardLayout_HeightMapResolution = worldData.BoardLayout.HeightMapResolution;

            float edgeEffectDistance = (float)worldData.BoardLayout.EdgeEffectDistance;//WorldSpecification.i.BoardLayout.EdgeEffectDistance;
            float num = 0f;
            float num2 = 0f;
            float num3 = edgeEffectDistance;
            bool edgeSetNorth = THUS.EdgeNorth!=0;
            if (edgeSetNorth)
            {
                float num4 = (float)(WorldSpecification_i_BoardLayout_HeightMapResolution - terrainX - 1);
                num4 = Mathf_Limited.Min(num4, edgeEffectDistance);
                num3 = Mathf_Limited.Min(num3, num4);
                num += (float)THUS.EdgeNorth * (edgeEffectDistance - num4);
                num2 += edgeEffectDistance - num4;
            }
            bool edgeSetEast = THUS.EdgeEast!=0;
            if (edgeSetEast)
            {
                float num5 = (float)(WorldSpecification_i_BoardLayout_HeightMapResolution - terrainY - 1);
                num5 = Mathf_Limited.Min(num5, edgeEffectDistance);
                num3 = Mathf_Limited.Min(num3, num5);
                num += (float)THUS.EdgeEast * (edgeEffectDistance - num5);
                num2 += edgeEffectDistance - num5;
            }
            bool edgeSetSouth = THUS.EdgeSouth!=0;
            if (edgeSetSouth)
            {
                float num6 = (float)terrainX;
                num6 = Mathf_Limited.Min(num6, edgeEffectDistance);
                num3 = Mathf_Limited.Min(num3, num6);
                num += (float)THUS.EdgeSouth * (edgeEffectDistance - num6);
                num2 += edgeEffectDistance - num6;
            }
            bool edgeSetWest = THUS.EdgeWest!=0;
            if (edgeSetWest)
            {
                float num7 = (float)terrainY;
                num7 = Mathf_Limited.Min(num7, edgeEffectDistance);
                num3 = Mathf_Limited.Min(num3, num7);
                num += (float)THUS.EdgeWest * (edgeEffectDistance - num7);
                num2 += edgeEffectDistance - num7;
            }
            bool flag = num2 > 0f;
            float num11;
            if (flag)
            {
                num /= num2;
                float num8 = Mathf_Limited.Lerp((float)THUS.BaseHeight, num, (edgeEffectDistance - num3) / edgeEffectDistance);
                float num9 = SMARTFUL_StaticWorld_BaseHeightToRelativeTerrainHeight(num8 - (float)THUS.HeightScale / 2f);
                float num10 = SMARTFUL_StaticWorld_BaseHeightToRelativeTerrainHeight(num8 + (float)THUS.HeightScale / 2f);
                num11 = num9 + inputValue * 0.5f * (num10 - num9);
            }
            else
            {
                num11 = SMARTFUL_TERRAIN_MinimumHeightmapValue((float)THUS.BaseHeight,(float)THUS.HeightScale) + inputValue * 0.5f * (SMARTFUL_TERRAIN_MaximumHeightmapValue((float)THUS.BaseHeight, (float)THUS.HeightScale) - SMARTFUL_TERRAIN_MinimumHeightmapValue((float)THUS.BaseHeight, (float)THUS.HeightScale));
            }
            return Mathf_Limited.Round(num11 * worldData.BoardLayout.WorldHeightAndDepth*2) / worldData.BoardLayout.WorldHeightAndDepth * 2;
        }

        public static float SMARTFUL_StaticWorld_BaseHeightToRelativeTerrainHeight(float baseHeight)
        {
            return Mathf_Limited.Clamp01(baseHeight / 2f + 0.5f);
        }

        public static float SMARTFUL_TERRAIN_MinimumHeightmapValue(float BaseHeight, float HeightScale)
        {
            return SMARTFUL_StaticWorld_BaseHeightToRelativeTerrainHeight(BaseHeight - HeightScale / 2f);
        }

        public static float SMARTFUL_TERRAIN_MaximumHeightmapValue(float BaseHeight, float HeightScale)
        {
            return SMARTFUL_StaticWorld_BaseHeightToRelativeTerrainHeight(BaseHeight + HeightScale / 2f);
        }


        public void SMARTFUL_HeightAndBiomeToAlpha(int splatX, int splatY, float height, /*enumBiome*/ int biome, float[,,] this_alphaMaps)
        {
            float num = Mathf_Limited.Lerp(1f, 0f, (height - 0.25f) * 2f);
            this_alphaMaps[splatX, splatY, 0] = num;
            this_alphaMaps[splatX, splatY, 1] = 0f;
            this_alphaMaps[splatX, splatY, 2] = 0f;
            this_alphaMaps[splatX, splatY, 3] = 0f;
            this_alphaMaps[splatX, splatY, 4] = 0f;
            switch (biome)
            {
                case 0://enumBiome.water:
                    this_alphaMaps[splatX, splatY, 0] = 1f;
                    break;
                case 1:// enumBiome.grassland:
                    this_alphaMaps[splatX, splatY, 1] = 1f - num;
                    break;
                case 2:// enumBiome.sand:
                    this_alphaMaps[splatX, splatY, 3] = 1f - num;
                    break;
                case 3:// enumBiome.snow:
                    this_alphaMaps[splatX, splatY, 2] = 1f - num;
                    break;
                case 4:// enumBiome.lava:
                    this_alphaMaps[splatX, splatY, 4] = 1f - num;
                    break;
            }
        }



        //Unmerges edges for a terrain
        public void SMARTFUL_TERRAININFO_UnmergeEdges(Terrain t)
        {
            t.EdgeNorth = 0f;
            t.EdgeSouth = 0f;
            t.EdgeWest = 0f;
            t.EdgeEast = 0f;
        }

        //merges edges of a terrain to all adjesent. Use second arg to not touch edges on same biome. If biome is different minimum gap is baseheight=-0.25
        public void SMARTFUL_TERRAININFO_MergeEdges(Terrain t,bool dontTouchSameBiome=false) //dontTouchSameBiome for the functionality of SetEdgesBasedOnBiome()
        {
            float a = -0.25f;
            /*BoardAndTerrainCoord boardAndTerrainCoordinates = this.BoardAndTerrainCoordinates;
            BoardAndTerrainCoord copy = boardAndTerrainCoordinates.GetCopy();
            BoardAndTerrainCoord boardAndTerrainCoord = copy;
            int num = boardAndTerrainCoord.TerrainArrayNorth;
            boardAndTerrainCoord.TerrainArrayNorth = num + 1;*/
            float[] temp = getCoordsByTerrain(t);
            Terrain Northern = FindTerrainAtPosition(new PointF(temp[0], temp[1] - 256));
            bool flag = Northern != null;//copy.CheckCoordinates();
            if (flag)
            {
                bool flag2 = Northern.Biome == t.Biome;
                if (flag2)
                {
                    if(dontTouchSameBiome)
                        t.EdgeNorth = (float)(Northern.BaseHeight + t.BaseHeight) / 2f;
                }
                else
                {
                    t.EdgeNorth = Mathf_Limited.Min(a, (float)(Northern.BaseHeight + t.BaseHeight) / 2f);
                }
            }
            /*copy = boardAndTerrainCoordinates.GetCopy();
            BoardAndTerrainCoord boardAndTerrainCoord2 = copy;
            num = boardAndTerrainCoord2.TerrainArrayNorth;
            boardAndTerrainCoord2.TerrainArrayNorth = num - 1;*/

            temp = getCoordsByTerrain(t);
            Terrain Southern = FindTerrainAtPosition(new PointF(temp[0], temp[1] + 256));
            bool flag3 = Southern != null;//copy.CheckCoordinates();
            if (flag3)
            {
                bool flag4 = Southern.Biome == t.Biome;
                if (flag4)
                {
                    if (dontTouchSameBiome)
                        t.EdgeSouth = (float)(Southern.BaseHeight + t.BaseHeight) / 2f;
                }
                else
                {
                    t.EdgeSouth = Mathf_Limited.Min(a, (float)(Southern.BaseHeight + t.BaseHeight) / 2f);
                }
            }
            /*copy = boardAndTerrainCoordinates.GetCopy();
            BoardAndTerrainCoord boardAndTerrainCoord3 = copy;
            num = boardAndTerrainCoord3.TerrainArrayEast;
            boardAndTerrainCoord3.TerrainArrayEast = num + 1;*/
            temp = getCoordsByTerrain(t);
            Terrain Eastern = FindTerrainAtPosition(new PointF(temp[0]+256, temp[1]));
            bool flag5 = Eastern != null;//copy.CheckCoordinates();
            if (flag5)
            {
                bool flag6 = Eastern.Biome == t.Biome;
                if (flag6)
                {
                    if (dontTouchSameBiome)
                        t.EdgeEast = (float)(Eastern.BaseHeight + t.BaseHeight) / 2f;
                }
                else
                {
                    t.EdgeEast = Mathf_Limited.Min(a, (float)(Eastern.BaseHeight + t.BaseHeight) / 2f);
                }
            }
            /*copy = boardAndTerrainCoordinates.GetCopy();
            BoardAndTerrainCoord boardAndTerrainCoord4 = copy;
            num = boardAndTerrainCoord4.TerrainArrayEast;
            boardAndTerrainCoord4.TerrainArrayEast = num - 1;*/
            temp = getCoordsByTerrain(t);
            Terrain Western = FindTerrainAtPosition(new PointF(temp[0] - 256, temp[1]));
            bool flag7 = Western != null;//copy.CheckCoordinates();
            if (flag7)
            {
                bool flag8 = Western.Biome == t.Biome;
                if (flag8)
                {
                    if (dontTouchSameBiome)
                        t.EdgeWest = (float)(Western.BaseHeight + t.BaseHeight) / 2f;
                }
                else
                {
                    t.EdgeWest = Mathf_Limited.Min(a, (float)(Western.BaseHeight + t.BaseHeight) / 2f);
                }
            }
        }


    }
}
