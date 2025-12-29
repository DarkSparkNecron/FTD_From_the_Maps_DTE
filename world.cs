using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTDMapgen_WinForms
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Text.Json.Serialization;

    public class WorldData
    {
        [JsonPropertyName("RedactorInfo")]
        public NonStaticProgramInfo RedactorInfo { get; set; }
        [JsonPropertyName("Physics")]
        public Physics Physics { get; set; }

        [JsonPropertyName("BoardLayout")]
        public BoardLayout BoardLayout { get; set; }

        [JsonPropertyName("Areas")]
        public AreasContainer Areas { get; set; }

        [JsonPropertyName("Weather")]
        public WeatherContainer Weather { get; set; }

        [JsonPropertyName("Phases")]
        public PhasesContainer Phases { get; set; }

        [JsonPropertyName("Unlocks")]
        public UnlocksContainer Unlocks { get; set; }

        [JsonPropertyName("AdventureModeSettings")]
        public AdventureModeSettings AdventureModeSettings { get; set; }

        [JsonPropertyName("GameConfiguration")]
        public GameConfiguration GameConfiguration { get; set; }
        [JsonPropertyName("DisplaySettings")]
        public DisplaySettings DisplaySettings { get; set; }
        [JsonPropertyName("Mountains")]
        public List<Mountain> mountains { get; set; }

        //BTW this method will crash if worldDataAdditional is smaller at any dimension than the WorldData
        public void copyFrom(WorldData worldDataAdditional, WorldCopyingParams settings)
        {
            //redactor info
            //displaySettings

            if (settings.CopyPhysics)Physics = worldDataAdditional.Physics;

            //mountains
            if (settings.addMountains) mountains.AddRange(worldDataAdditional.mountains);

            if (settings.CopyAreaWeatherTemplates==AreaWeatherCopySetting.ReplaceAreaWeather || settings.CopyAreaWeatherTemplates == AreaWeatherCopySetting.ReplaceAreaAddWeather) Areas = worldDataAdditional.Areas;
            if (settings.CopyAreaWeatherTemplates == AreaWeatherCopySetting.ReplaceAreaWeather || settings.CopyAreaWeatherTemplates == AreaWeatherCopySetting.AddAreaReplaceWeather)
            {
                Weather = worldDataAdditional.Weather;
            }
            if (settings.CopyAreaWeatherTemplates == AreaWeatherCopySetting.AddAreaAddWeather || settings.CopyAreaWeatherTemplates == AreaWeatherCopySetting.AddAreaReplaceWeather)
            {
                Areas.Areas.AddRange(worldDataAdditional.Areas.Areas);
            }
            if (settings.CopyAreaWeatherTemplates == AreaWeatherCopySetting.AddAreaAddWeather || settings.CopyAreaWeatherTemplates == AreaWeatherCopySetting.ReplaceAreaAddWeather)
            {
                Weather.Weathers.AddRange(worldDataAdditional.Weather.Weathers);
            }
            if (settings.CopyPhases == CopySetting.Replace) Phases = worldDataAdditional.Phases;
            if (settings.CopyPhases == CopySetting.Replace) 
            {
                Phases.Phases.AddRange(worldDataAdditional.Phases.Phases);
                Phases.PhaseCount += worldDataAdditional.Phases.PhaseCount;
            }

            if (settings.CopyUtilitySran)
            {
                Unlocks = worldDataAdditional.Unlocks;
                AdventureModeSettings = worldDataAdditional.AdventureModeSettings;
            }

            if (settings.CopyGameConfig) GameConfiguration = worldDataAdditional.GameConfiguration;

            //board layout
            if (settings.CopyGlobalTerrainSettings)
            {
                BoardLayout.HeightMapResolution = worldDataAdditional.BoardLayout.HeightMapResolution;
                BoardLayout.EdgeEffectDistance = worldDataAdditional.BoardLayout.EdgeEffectDistance;
                BoardLayout.StitchWidth = worldDataAdditional.BoardLayout.StitchWidth;
                BoardLayout.WorldHeightAndDepth = worldDataAdditional.BoardLayout.WorldHeightAndDepth;

                BoardLayout.TerrainSize = worldDataAdditional.BoardLayout.TerrainSize;
            }

            bool initReferencelessTerrain= BoardLayout.TerrainsPerBoard>worldDataAdditional.BoardLayout.TerrainsPerBoard;
            int oldSize = BoardLayout.TerrainsPerBoard;
            int newSize = worldDataAdditional.BoardLayout.TerrainsPerBoard;


            if (!settings.BoardReshape) this.reshapeMap(worldDataAdditional.BoardLayout.BoardSections[0].Count, worldDataAdditional.BoardLayout.BoardSections.Count, worldDataAdditional.BoardLayout.TerrainsPerBoard);

            if (BoardLayout != null && false)
            {
                int boardsY = BoardLayout.BoardSections.Count; //old sizes
                int boardsX = boardsY > 0 ? BoardLayout.BoardSections[0].Count : 0;

                int refBoardsY= worldDataAdditional.BoardLayout.BoardSections.Count; //new sizes
                int refBoardsX = refBoardsY > 0 ? worldDataAdditional.BoardLayout.BoardSections[0].Count : 0;

                
                for (int boardY = 0; boardY < boardsY; boardY++)
                {
                    var boardRow = BoardLayout.BoardSections[boardY]; //old row size
                    var refBoardRow = worldDataAdditional.BoardLayout.BoardSections[boardY]; //new row size
                    //NULLIFY ROW IF SIZE GETS SMALLER AND CONTINUE
                    for (int boardX = 0; boardX < boardRow.Count; boardX++)
                    {
                        var boardSection = boardRow[boardX]; //old row length
                        var refSection = worldDataAdditional.BoardLayout.BoardSections[boardY][boardX]; //ПОД ЧЕМ ТЫ ЭТО ПИСАЛ ВООБЩЕ??? ладно отмена паники
                        
                        if (settings.CopyBoardLayoutData)
                        {
                            boardSection.AreaId = refSection.AreaId;
                            boardSection.GarrisonLocation = refSection.GarrisonLocation;
                            boardSection.LandGarrisonLocation = refSection.LandGarrisonLocation;
                        }

                        //NLLIFY BOARD SECTION IF THE SIZE GETS SMALLER AND CONTINUE

                        if (boardSection?.Terrains == null) continue;

                        //terrains are not setted for ref yet
                        int terrainsY = boardSection.Terrains.Count;
                        int terrainsX = terrainsY > 0 ? boardSection.Terrains[0].Count : 0;

                        for (int terrainY = 0; terrainY < terrainsY; terrainY++)
                        {
                            var terrainRow = boardSection.Terrains[terrainY];
                            var refTerrainRow = refSection.Terrains[terrainY];
                            //NULLIFY ROW IF SIZE GETS SMALLER AND CONTINUE
                            for (int terrainX = 0; terrainX < terrainsX; terrainX++)
                            {
                                var terrain = terrainRow[terrainX];
                                var refTerrain = refTerrainRow[terrainX];

                                if (terrain == null)
                                { 
                                    terrain = new Terrain();
                                    terrain.initSimple();
                                }

                                //situation where there is nothing to copy
                                if((terrainY> worldDataAdditional.BoardLayout.TerrainsPerBoard-1) || (terrainX > worldDataAdditional.BoardLayout.TerrainsPerBoard - 1))
                                {
                                    continue;
                                }else
                                {
                                    if (settings.CopyTerrainPerlin)
                                    {
                                        terrain.PerlinFrequency = refTerrain.PerlinFrequency;
                                        terrain.PerlinOctaves = refTerrain.PerlinOctaves;
                                    }
                                    if (settings.CopyTerrainEdges)
                                    {
                                        terrain.EdgeNorth = refTerrain.EdgeNorth;
                                        terrain.EdgeSouth = refTerrain.EdgeSouth;
                                        terrain.EdgeEast = refTerrain.EdgeEast;
                                        terrain.EdgeWest = refTerrain.EdgeWest;
                                    }
                                    if(settings.CopyTerrainSeed) terrain.Seed = refTerrain.Seed;
                                    if(settings.CopyTerrainLiterally)
                                    {
                                        terrain.copyDataFrom(refTerrain);
                                    }
                                }

                                //NLLIFY IF THE SIZE GETS SMALLER AND CONTINUE

                            }
                        }
                    }
                }
            }

            //if (settings.BoardReshape && (BoardLayout.TerrainsPerBoard == 0 || BoardLayout.TerrainsPerBoard < worldDataAdditional.BoardLayout.TerrainsPerBoard)) BoardLayout.TerrainsPerBoard = worldDataAdditional.BoardLayout.TerrainsPerBoard;

        }


        public void reshapeMap(int newBoardX, int newBoardY, int newTerrainsPerBoard)
        {
            

            
            int oldSize = BoardLayout.TerrainsPerBoard;
            
            //SHRINK MAP
            if (BoardLayout != null)
            {
                int boardsY = BoardLayout.BoardSections.Count; //old sizes
                int boardsX = boardsY > 0 ? BoardLayout.BoardSections[0].Count : 0;




                for (int boardY = boardsY-1; boardY >= newBoardY*0; boardY--)
                {
                    var boardRow = BoardLayout.BoardSections[boardY]; //old row size
                    
                    //NULLIFY ROW IF SIZE GETS SMALLER AND CONTINUE
                    for (int boardX = boardRow.Count-1; boardX >= boardRow.Count*0; boardX--)
                    {
                        var boardSection = boardRow[boardX]; //old row length
                        
                        //if (boardSection?.Terrains == null) continue;

                        //terrains are not setted for ref yet
                        int terrainsY = boardSection.Terrains.Count;
                        int terrainsX = terrainsY > 0 ? boardSection.Terrains[0].Count : 0;

                        for (int terrainY = terrainsY-1; terrainY >= newTerrainsPerBoard; terrainY--)
                        {
                            var terrainRow = boardSection.Terrains[terrainY];
                            
                            for (int terrainX = terrainsX-1; terrainX >= newTerrainsPerBoard; terrainX--)
                            {
                                var terrain = terrainRow[terrainX];
                                

                                if (terrain == null && false)
                                {
                                    terrain = new Terrain();
                                    terrain.initSimple();
                                }

                                if(terrainX>= newTerrainsPerBoard && false)
                                {
                                    terrainRow.Remove(terrain);
                                    terrain = null;
                                }

                            }
                            if (terrainY >= newTerrainsPerBoard && false)
                            {
                                boardSection.Terrains.Remove(terrainRow);
                                terrainRow = null;
                            }
                        }

                        if (boardX >= newBoardX)
                        {
                            boardRow.Remove(boardSection);
                            boardSection = null;
                        }


                    }

                    if (boardY >= newBoardY)
                    {
                        BoardLayout.BoardSections.Remove(boardRow);
                        boardRow = null;
                    }


                }

                
                foreach(List<BoardSection> LBS in BoardLayout.BoardSections)
                {
                    foreach(BoardSection BS in LBS)
                    {
                        int terrainsY = BS.Terrains.Count;
                        int terrainsX = terrainsY > 0 ? BS.Terrains[0].Count : 0;
                        for (int terrainY = terrainsY-1; terrainY >= newTerrainsPerBoard*0; terrainY--)
                        {
                            var terrainRow = BS.Terrains[terrainY];
                            terrainsX = terrainRow.Count;
                            if (true)
                            for (int terrainX = terrainsX-1; terrainX >= newTerrainsPerBoard*0; terrainX--)
                            {
                                var terrain = terrainRow[terrainX];


                                if (terrain == null && false)
                                {
                                    terrain = new Terrain();
                                    terrain.initSimple();
                                }

                                if (terrainX >= newTerrainsPerBoard)
                                {
                                    terrainRow.Remove(terrain);
                                    terrain = null;
                                }

                            }
                            if (terrainY >= newTerrainsPerBoard)
                            {
                                BS.Terrains.Remove(terrainRow);
                                terrainRow = null;
                            }
                        }
                    }
                }
                BoardLayout.TerrainsPerBoard = newTerrainsPerBoard;
            }

            //EXPAND MAP
            if (BoardLayout != null)
            {
                int boardsY = BoardLayout.BoardSections.Count; //old sizes
                int boardsX = boardsY > 0 ? BoardLayout.BoardSections[0].Count : 0;

                


                for (int boardY = 0; boardY < newBoardY; boardY++)
                {

                    List<BoardSection> boardRow;
                    if(boardY >= boardsY)
                    {
                        boardRow = new List<BoardSection>();
                        BoardLayout.BoardSections.Add(boardRow);
                    }
                    else
                    {
                        boardRow = BoardLayout.BoardSections[boardY];
                    }
                    
                    
                    for (int boardX = 0; boardX < newBoardX; boardX++)
                    {
                        BoardSection boardSection;
                        if (boardX >= boardRow.Count)
                        {
                            boardSection = new BoardSection();
                            boardSection.AreaId = new AreaId();
                            boardSection.AreaId.Id = 0;
                            boardSection.LandGarrisonLocation = null;//?..
                            boardSection.garrisonVectorToStringIdiotic();
                            boardSection.Terrains = new List<List<Terrain>>();
                            boardRow.Add(boardSection);
                        }
                        else
                        {
                            boardSection = boardRow[boardX];
                        }
                        

                        int terrainsY = boardSection.Terrains.Count;
                        int terrainsX = terrainsY > 0 ? boardSection.Terrains[0].Count : 0;
                        if(true)
                        for (int terrainY = terrainsY*0; terrainY < newTerrainsPerBoard; terrainY++) //vertical lines
                        {

                            List<Terrain> terrainRow;
                            if (terrainY >= oldSize || terrainsY==0)
                            {
                                terrainRow = new List<Terrain>();
                                boardSection.Terrains.Add(terrainRow);
                            }
                            else
                            {
                                terrainRow = boardSection.Terrains[terrainY];
                            }

                            int initCount = terrainRow.Count;
                            for (int terrainX = initCount; terrainX < newTerrainsPerBoard; terrainX++)
                            {
                                Terrain terrain;
                                if (terrainX >= oldSize || initCount == 0)
                                {
                                    terrain = new Terrain();
                                    terrain.initSimple();
                                    terrainRow.Add(terrain);
                                }
                                else
                                {
                                    terrain = terrainRow[terrainX];
                                    //and idk, because everything is fine with that terrain
                                }


                            }
                        }
                    }
                }
            }

            //DELETE MOUNTAINS OUT OF BOUNDS
            //mountain position is stored in 256 terrain size, but lied in GUI
            int maxX = newBoardX * newTerrainsPerBoard * 256;
            int maxY = newBoardY * newTerrainsPerBoard * 256;
            foreach (Mountain m in this.mountains)
            {
                if(m.Position.X>maxX || m.Position.Y>maxY)
                {
                    this.mountains.Remove(m);
                    //it is not giving me an ability to set m to null. I care for your memory!
                }
            }

        }
    }

    public class Physics
    {
        [JsonPropertyName("SpaceBegins")]
        public double SpaceBegins { get; set; }

        [JsonPropertyName("SpaceEnds")]
        public double SpaceEnds { get; set; }

        [JsonPropertyName("SpaceRestarts")]
        public double SpaceRestarts { get; set; }

        [JsonPropertyName("SpaceIsFullAgain")]
        public double SpaceIsFullAgain { get; set; }

        [JsonPropertyName("SpaceFallOffMode")]
        public int SpaceFallOffMode { get; set; }

        [JsonPropertyName("AirDensityBeings")]
        public double AirDensityBeings { get; set; }

        [JsonPropertyName("AirDensityEnds")]
        public double AirDensityEnds { get; set; }

        [JsonPropertyName("AirDensityFallOffMode")]
        public int AirDensityFallOffMode { get; set; }

        [JsonPropertyName("WaterLevel")]
        public double WaterLevel { get; set; }

        [JsonPropertyName("WindDirectionAndMagnitude")]
        public string WindDirectionAndMagnitude { get; set; }

        [JsonPropertyName("FlatWorld")]
        public bool FlatWorld { get; set; }

        [JsonPropertyName("HorizonHeight")]
        public double HorizonHeight { get; set; }

        [JsonPropertyName("GravityConstant")]
        public double GravityConstant { get; set; }

        [JsonPropertyName("BuoyancyUpForcePerVolume")]
        public double BuoyancyUpForcePerVolume { get; set; }

        [JsonPropertyName("SensibleMinimumAltitude")]
        public double SensibleMinimumAltitude { get; set; }

        [JsonPropertyName("VisibleRadiusMaximum")]
        public double VisibleRadiusMaximum { get; set; }

        [JsonPropertyName("PlanetName")]
        public string PlanetName { get; set; }

        [JsonPropertyName("PlanetCentre")]
        public string PlanetCentre { get; set; }

        [JsonPropertyName("PlanetRadius")]
        public double PlanetRadius { get; set; }

        [JsonPropertyName("HorizonDiameter")]
        public double HorizonDiameter { get; set; }
    }

    public class BoardLayout
    {
        [JsonPropertyName("TerrainSize")]
        public double TerrainSize { get; set; }

        [JsonPropertyName("HeightMapResolution")]
        public int HeightMapResolution { get; set; }

        [JsonPropertyName("TerrainsPerBoard")]
        public int TerrainsPerBoard { get; set; }

        [JsonPropertyName("BoardSections")]
        public List<List<BoardSection>> BoardSections { get; set; }

        [JsonPropertyName("WorldHeightAndDepth")]
        public int WorldHeightAndDepth { get; set; }

        [JsonPropertyName("StitchWidth")]
        public int StitchWidth { get; set; }

        [JsonPropertyName("EdgeEffectDistance")]
        public double EdgeEffectDistance { get; set; }
    }

    public class BoardSection
    {
        [JsonPropertyName("AreaId")]
        public AreaId AreaId { get; set; }

        [JsonPropertyName("Terrains")]
        public List<List<Terrain>> Terrains { get; set; }

        [JsonPropertyName("GarrisonLocation")]
        public string GarrisonLocation { get; set; }

        [JsonPropertyName("LandGarrisonLocation")]
        public Vector3Data LandGarrisonLocation { get; set; }

        public void garrisonVectorToStringIdiotic()
        {
            if(LandGarrisonLocation!=null)
            {
                GarrisonLocation = LandGarrisonLocation.X + "," + LandGarrisonLocation.Y +","+ LandGarrisonLocation.Z;
            }
            else
            {
                GarrisonLocation = "0,0,0";
            }
        }
    }

    public class AreaId
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }
    }

    public class Terrain
    {
        [JsonPropertyName("Biome")]
        public int Biome { get; set; }

        [JsonPropertyName("HeightScale")]
        public double HeightScaleFR { get; set; }

        [JsonPropertyName("Seed")]
        public int Seed { get; set; }

        [JsonPropertyName("BaseHeight")]
        public double BaseHeightFR { get; set; }

        [JsonPropertyName("PerlinFrequency")]
        public int PerlinFrequency { get; set; }

        [JsonPropertyName("PerlinOctaves")]
        public int PerlinOctaves { get; set; }

        [JsonPropertyName("EdgeNorth")]
        public double EdgeNorth { get; set; }

        [JsonPropertyName("EdgeSouth")]
        public double EdgeSouth { get; set; }

        [JsonPropertyName("EdgeEast")]
        public double EdgeEast { get; set; }

        [JsonPropertyName("EdgeWest")]
        public double EdgeWest { get; set; }
        [JsonPropertyName("BaseHeight0")]
        public double? BaseHeight { get; set; }
        [JsonPropertyName("HeightScale0")]
        public double? HeightScale { get; set; }

        public void copyDataFrom(Terrain t)
        {
            this.Biome = t.Biome;
            this.BaseHeight = t.BaseHeight;
            this.HeightScale = t.HeightScale;

            this.BaseHeightFR = t.BaseHeightFR;
            this.HeightScaleFR = t.HeightScaleFR;

            this.Seed = t.Seed;

            this.PerlinFrequency = t.PerlinFrequency;
            this.PerlinOctaves = t.PerlinOctaves;

            this.EdgeEast = t.EdgeEast;
            this.EdgeNorth = t.EdgeNorth;
            this.EdgeSouth = t.EdgeSouth;
            this.EdgeWest = t.EdgeWest;
        }

        public void newSeed()
        {
            Random rnd = new Random();
            Seed = rnd.Next(100, 10000);
        }
        public void perlinForDummies()
        {
            PerlinFrequency = 4;
            PerlinOctaves = 5;
        }
        public void initSimple()
        {
            this.Biome = 0;
            this.BaseHeight = 0;
            this.HeightScale = 0;

            this.BaseHeightFR = 0;
            this.HeightScaleFR = 0;

            this.newSeed();
            this.perlinForDummies();

            //idk how it work honestly. It is terrain smoothing probably
            this.EdgeEast = 0;
            this.EdgeNorth = 0;
            this.EdgeSouth = 0;
            this.EdgeWest = 0;
        }
    }

    public class Vector3Data
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("z")]
        public double Z { get; set; }

        [JsonPropertyName("normalized")]
        public string Normalized { get; set; }

        [JsonPropertyName("magnitude")]
        public double Magnitude { get; set; }

        [JsonPropertyName("sqrMagnitude")]
        public double SqrMagnitude { get; set; }
    }

    public class AreasContainer
    {
        [JsonPropertyName("Areas")]
        public List<Area> Areas { get; set; }
    }

    public class Area
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("Weathers")]
        public List<WeatherProbability> Weathers { get; set; }

        [JsonPropertyName("Id")]
        public AreaId Id { get; set; }

        [JsonPropertyName("Color")]
        public string Color { get; set; }
    }

    public class WeatherProbability
    {
        [JsonPropertyName("SettingId")]
        public SettingId SettingId { get; set; }

        [JsonPropertyName("Probability")]
        public double Probability { get; set; }

        [JsonPropertyName("MinDuration")]
        public double MinDuration { get; set; }

        [JsonPropertyName("MaxDuration")]
        public double MaxDuration { get; set; }
    }

    public class SettingId
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }
    }

    public class WeatherContainer
    {
        [JsonPropertyName("Weathers")]
        public List<WeatherSetting> Weathers { get; set; }
    }

    public class WeatherSetting
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("Id")]
        public SettingId Id { get; set; }

        [JsonPropertyName("LerpTime")]
        public double LerpTime { get; set; }

        [JsonPropertyName("ClarityFactor")]
        public double ClarityFactor { get; set; }

        [JsonPropertyName("WaveSpeed")]
        public double WaveSpeed { get; set; }

        [JsonPropertyName("BadnessMetric")]
        public double BadnessMetric { get; set; }

        [JsonPropertyName("CloudCover")]
        public double CloudCover { get; set; }

        [JsonPropertyName("PrecipitationLevel")]
        public double PrecipitationLevel { get; set; }

        [JsonPropertyName("PrecipitationLevel_StormLayer")]
        public double PrecipitationLevelStormLayer { get; set; }

        [JsonPropertyName("StormCloudCover")]
        public double StormCloudCover { get; set; }

        [JsonPropertyName("RainLevel")]
        public double RainLevel { get; set; }

        [JsonPropertyName("RainSoundFxLevel")]
        public double RainSoundFxLevel { get; set; }

        [JsonPropertyName("WindSoundFxLevel")]
        public double WindSoundFxLevel { get; set; }

        [JsonPropertyName("GlowVariance")]
        public double GlowVariance { get; set; }

        [JsonPropertyName("GlowVariance_StormLayer")]
        public double GlowVarianceStormLayer { get; set; }

        [JsonPropertyName("ViewDistance")]
        public double ViewDistance { get; set; }

        [JsonPropertyName("MistLevel")]
        public double MistLevel { get; set; }

        [JsonPropertyName("SnowLevel")]
        public double SnowLevel { get; set; }

        [JsonPropertyName("ScatteringRadius")]
        public double ScatteringRadius { get; set; }

        [JsonPropertyName("SunShaftIntensity")]
        public double SunShaftIntensity { get; set; }

        [JsonPropertyName("SunShaftColor")]
        public string SunShaftColor { get; set; }

        [JsonPropertyName("LightningFrequency")]
        public double LightningFrequency { get; set; }

        [JsonPropertyName("StormLevel")]
        public double StormLevel { get; set; }

        [JsonPropertyName("StormSoundFxLevel")]
        public double StormSoundFxLevel { get; set; }

        [JsonPropertyName("FogColorAndHorizonAlpha")]
        public string FogColorAndHorizonAlpha { get; set; }

        [JsonPropertyName("AmbientLightColor")]
        public string AmbientLightColor { get; set; }

        [JsonPropertyName("AmbientLightIntensity")]
        public double AmbientLightIntensity { get; set; }

        [JsonPropertyName("FogDensity")]
        public double FogDensity { get; set; }

        [JsonPropertyName("WaterReflectionModifier")]
        public double WaterReflectionModifier { get; set; }

        [JsonPropertyName("WaterOpacityModifier")]
        public double WaterOpacityModifier { get; set; }

        [JsonPropertyName("WaterColorModifier")]
        public string WaterColorModifier { get; set; }

        [JsonPropertyName("SubsurfaceWaterColorModifier")]
        public string SubsurfaceWaterColorModifier { get; set; }

        [JsonPropertyName("CpuOceanWaterColorModifier")]
        public string CpuOceanWaterColorModifier { get; set; }

        [JsonPropertyName("WindModifier")]
        public double WindModifier { get; set; }

        [JsonPropertyName("WaveAmplitude")]
        public double WaveAmplitude { get; set; }

        [JsonPropertyName("WaveFrequency")]
        public double WaveFrequency { get; set; }

        [JsonPropertyName("WaveChop")]
        public double WaveChop { get; set; }

        [JsonPropertyName("WaveSmoothness")]
        public double WaveSmoothness { get; set; }

        [JsonPropertyName("DeepFoamThreshold")]
        public double DeepFoamThreshold { get; set; }

        [JsonPropertyName("CubemapName")]
        public string CubemapName { get; set; }

        [JsonPropertyName("SongHashTag")]
        public string SongHashTag { get; set; }
    }

    public class PhasesContainer
    {
        [JsonPropertyName("Phases")]
        public List<Phase> Phases { get; set; }

        [JsonPropertyName("PhaseCount")]
        public int PhaseCount { get; set; }
    }

    public class Phase
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("End")]
        public double End { get; set; }

        [JsonPropertyName("SpeedOfTime")]
        public double SpeedOfTime { get; set; }

        [JsonPropertyName("SunShaftIntensityFactor")]
        public double SunShaftIntensityFactor { get; set; }

        [JsonPropertyName("SunShaftColor")]
        public string SunShaftColor { get; set; }

        [JsonPropertyName("AmbientLightColor")]
        public string AmbientLightColor { get; set; }

        [JsonPropertyName("AmbientLightIntensity")]
        public double AmbientLightIntensity { get; set; }

        [JsonPropertyName("HorizonColor")]
        public string HorizonColor { get; set; }

        [JsonPropertyName("WaterColor")]
        public string WaterColor { get; set; }

        [JsonPropertyName("WaterOpacity")]
        public double WaterOpacity { get; set; }

        [JsonPropertyName("WaterReflectivity")]
        public double WaterReflectivity { get; set; }

        [JsonPropertyName("CloudBrightnessFactor")]
        public double CloudBrightnessFactor { get; set; }

        [JsonPropertyName("CubemapName")]
        public string CubemapName { get; set; }

        [JsonPropertyName("CubemapReflection")]
        public string CubemapReflection { get; set; }
    }

    public class UnlocksContainer
    {
        [JsonPropertyName("Unlocks")]
        public List<object> Unlocks { get; set; }
    }

    public class AdventureModeSettings
    {
        [JsonPropertyName("MaterialGrowthMin")]
        public int MaterialGrowthMin { get; set; }

        [JsonPropertyName("MaterialGrowthMax")]
        public int MaterialGrowthMax { get; set; }

        [JsonPropertyName("RZRadiusMin")]
        public int RZRadiusMin { get; set; }

        [JsonPropertyName("RZRadiusMax")]
        public int RZRadiusMax { get; set; }

        [JsonPropertyName("RZStartAmount")]
        public int RZStartAmount { get; set; }

        [JsonPropertyName("RZStartGrowthAmount")]
        public int RZStartGrowthAmount { get; set; }

        [JsonPropertyName("MaterialAmountMin")]
        public int MaterialAmountMin { get; set; }

        [JsonPropertyName("MaterialAmountMax")]
        public int MaterialAmountMax { get; set; }
    }

    public class GameConfiguration
    {
        [JsonPropertyName("AttributeDictionary")]
        public AttributeDictionary AttributeDictionary { get; set; }
    }

    public class AttributeDictionary
    {
        [JsonPropertyName("_listOfNames")]
        public object ListOfNames { get; set; }

        [JsonPropertyName("_listOfObjects")]
        public object ListOfObjects { get; set; }
    }
}
