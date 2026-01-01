using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FTDMapgen_WinForms
{
    public class AreaPrefab
    {
        [JsonPropertyName("RedactorInfo")]
        public NonStaticProgramInfo RedactorInfo { get; set; }
        [JsonPropertyName("Size")]
        public Point Size { get; set; }
        //ACHTUNG: В отличии от смарта в этой матрице террейны хранятся в локальных координатах (0,0) слева-напрво  (Terrain - X), сверху вниз (List<Terrain>> - Y - строки Террейнов)
        [JsonPropertyName("Terrains")]
        public List<List<Terrain>> Terrains { get; set; }
        [JsonPropertyName("Mountains")]
        public List<Mountain> mountains { get; set; }
        

        //Дано выделение, штука даеет суперсмарченную координату террейна в нем, или нулл
        public PointF getLocalCoordWithinSelection(PointF upperLeft, PointF habariteData, Terrain t, MainForm MF)
        {
            Point ans = new Point();
            float[] Coordpair = MF.getCoordsByTerrain(t);
            bool withinArea = true;
            if (Coordpair[0] < upperLeft.X || Coordpair[0] > (upperLeft.X + habariteData.X)) withinArea = false;
            if (Coordpair[1] < upperLeft.Y || Coordpair[1] > (upperLeft.Y + habariteData.Y)) withinArea = false;
            if(withinArea)
            {
                ans.X = (int)((Coordpair[0] - upperLeft.X) / 256);
                ans.Y = (int)((Coordpair[1] - upperLeft.Y) / 256);
            }
            else
            {
                MessageBox.Show($"Error getting coords for terrain  withing area, because it is not in that area. Returned (0,0)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Point.Empty; //its just (0,0)
            }
            return ans; //да, он возвращает pointF из Point как-то
        }

        //исходя из выделения говорит глобальные координаты для вставки
        public PointF getGlobaCoordForSelection(int localX, int localY, PointF upperLeft, PointF habariteData)
        {
            PointF ans = new PointF();
            ans.X = upperLeft.X + (localX+1) * 256; //WHY????
            ans.Y = upperLeft.Y + localY * 256;
            if(ans.X> habariteData.X+upperLeft.X || ans.Y > habariteData.Y + upperLeft.Y)
            {
                MessageBox.Show($"Error. Terrain is out of bounds for that area:"+ans, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return ans;
        }

        public List<List<Terrain>> getTerrainsWithingSelection(PointF upperLeft, PointF habariteData, MainForm MF)
        {
            int wdth = (int)(habariteData.X / 256);
            int hgth = (int)(habariteData.Y / 256);
            //int strokK = 0;
            //int withingStroK = 0;
            List<List<Terrain >> ans = new List<List<Terrain>>(hgth);
            for (int i = 0; i < hgth; i++)
            {
                ans.Add(new List<Terrain>(wdth)); //because ans[0] is null
                for (int j = 0; j < wdth; j++)
                    ans[i].Add(new Terrain()); //i dont want to fix same error twice
            }
            foreach(List<BoardSection> LBS in MF.worldData.BoardLayout.BoardSections)
            {
                foreach (BoardSection BS in LBS)
                {
                    foreach(List<Terrain> LT in BS.Terrains)
                    {
                        foreach(Terrain t in LT)
                        {
                            float[] Coordpair = MF.getCoordsByTerrain(t);
                            bool withinArea = true;
                            if (Coordpair[0] < upperLeft.X || Coordpair[0] >= (upperLeft.X + habariteData.X)) withinArea = false;
                            if (Coordpair[1] < upperLeft.Y || Coordpair[1] >= (upperLeft.Y + habariteData.Y)) withinArea = false;
                            if (withinArea)
                            {
                                int X = (int)((Coordpair[0] - upperLeft.X) / 256); //0<->X.size = error
                                int Y = (int)((Coordpair[1] - upperLeft.Y) / 256); //0<->Y.size = error
                                if (X >= 0 && Y >= 0) //oh well it actually goes futher that array size.. so if is wrong
                                {
                                    ans[Y][X] = new Terrain();
                                    ans[Y][X].copyDataFrom(t);
                                }
                            }
                        }
                    }
                }
            }

            return ans;
        }

        public void insertIntoSelection(AreaPrefab prefb, PointF upperLeft, PointF habariteData, MainForm MF)
        {
            bool canDo = true;
            if(habariteData.X/256!=prefb.Size.X || habariteData.Y / 256 != prefb.Size.Y)
            {
                //incorrect area selection to insert
                canDo = false;
            }

            if(canDo)
            for (int i = 0; i < prefb.Size.Y; i++)
            {
                for (int j = 0; j < prefb.Size.X; j++)
                {
                    PointF GlobalCoords = getGlobaCoordForSelection(j, i, upperLeft, habariteData);
                    Terrain t = MF.FindTerrainAtPosition(GlobalCoords);
                    if (t != null) t.copyDataFrom(prefb.Terrains[i][j]);
                }
            }
            if (canDo)
                this.addMountainsToSelection(upperLeft, habariteData, MF); //idk why it ended up like that but whatever i want it to work
        }

        public void loadMountainsFromSelection(PointF upperLeft, PointF habariteData, MainForm MF)
        {
            this.mountains = new List<Mountain>();
            foreach(Mountain m in MF.worldData.mountains)
            {
                bool x = m.Position.X >= upperLeft.X && m.Position.X <= (upperLeft.X + habariteData.X);
                bool y = m.Position.Y >= upperLeft.Y && m.Position.Y <= (upperLeft.Y + habariteData.Y);
                if (x && y)
                {
                    Mountain normalised = new Mountain();
                    normalised.copyDataFrom(m); //its actually not normalised, just moved
                    PointF normPos = new PointF(normalised.Position.X - upperLeft.X, normalised.Position.Y - upperLeft.Y);
                    normalised.Position = normPos;
                    this.mountains.Add(normalised);
                }
            }
        }

        public void addMountainsToSelection(PointF upperLeft, PointF habariteData, MainForm MF)
        {
            foreach (Mountain m in this.mountains)
            {
                bool x = m.Position.X >= 0 && m.Position.X <= habariteData.X;
                bool y = m.Position.Y >= 0 && m.Position.Y <=  habariteData.Y;
                if (x && y)
                {
                    Mountain denormalised = new Mountain();
                    denormalised.copyDataFrom(m); //its actually not denormalised, just moved
                    PointF denormPos = new PointF(denormalised.Position.X + upperLeft.X, denormalised.Position.Y + upperLeft.Y);
                    denormalised.Position = denormPos;
                    MF.worldData.mountains.Add(denormalised);
                }
            }
        }

    }
}
