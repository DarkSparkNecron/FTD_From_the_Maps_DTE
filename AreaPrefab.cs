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
        [JsonPropertyName("Mountains")]
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
            ans.X = upperLeft.X + localX * 256;
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
            for(int i=0;i<wdth;i++)
                ans[i] = new List<Terrain>(wdth);
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
                            if (Coordpair[0] < upperLeft.X || Coordpair[0] > (upperLeft.X + habariteData.X)) withinArea = false;
                            if (Coordpair[1] < upperLeft.Y || Coordpair[1] > (upperLeft.Y + habariteData.Y)) withinArea = false;
                            if (withinArea)
                            {
                                int X = (int)((Coordpair[0] - upperLeft.X) / 256);
                                int Y = (int)((Coordpair[1] - upperLeft.Y) / 256);
                                ans[Y][X] = new Terrain();
                                ans[Y][X].copyDataFrom(t);
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
        }


        public void fillSelection(PointF upperLeft, PointF habariteData, MainForm MF)
        {
            for(int i=(int)upperLeft.Y;i<(int)(upperLeft.Y+habariteData.Y);i+=256)
            {
                for (int j = (int)upperLeft.X; j < (int)(upperLeft.X + habariteData.X); j += 256)
                {
                    MF.ApplyBrush(new PointF(j, i));
                }
            }
        }

        public void deleteMountainsInArea(PointF upperLeft, PointF habariteData, MainForm MF)
        {
            foreach (Mountain m in MF.worldData.mountains)
            {
                bool x = m.Position.X >= upperLeft.X && m.Position.X <= (upperLeft.X + habariteData.X);
                bool y = m.Position.Y >= upperLeft.Y && m.Position.Y <= (upperLeft.Y + habariteData.Y);
                if (x && y) MF.worldData.mountains.Remove(m);
            }
        }

        public void loadMountainsFromSelection(PointF upperLeft, PointF habariteData, MainForm MF)
        {
            foreach(Mountain m in MF.worldData.mountains)
            {
                bool x = m.Position.X >= upperLeft.X && m.Position.X <= (upperLeft.X + habariteData.X);
                bool y = m.Position.Y >= upperLeft.Y && m.Position.Y <= (upperLeft.Y + habariteData.Y);
                if (x && y) this.mountains.Add(m);
            }
        }

        public void addMountainsToSelection(PointF upperLeft, PointF habariteData, MainForm MF)
        {
            foreach (Mountain m in MF.worldData.mountains)
            {
                bool x = m.Position.X >= upperLeft.X && m.Position.X <= (upperLeft.X + habariteData.X);
                bool y = m.Position.Y >= upperLeft.Y && m.Position.Y <= (upperLeft.Y + habariteData.Y);
                if (x && y) MF.worldData.mountains.Add(m);
            }
        }

    }
}
