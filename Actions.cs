using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTDMapgen_WinForms
{
    public abstract class Action
    {
        public int ActionId;

        public abstract void Redo();
        public abstract void Undo();

        /*
         TODO
           EditSelectedTerrain (on selection save an example of selected terrain. When new selection is done, if terrain has different params -> add Action)
           EditSelectedMountain (same as terrain, but for mountains)
           AddMountain
           Delete Mountain
           Delete Mountains in area
           Brush
           Fill
           Insert Prefab
         
         
         */
    }

    public class ActionChangeTerrainProperty : Action
    {
        public Terrain modifiedTerrainLink;
        public Terrain originalDataStorage;
        public Terrain newDataStorage;

        public ActionChangeTerrainProperty(Terrain tileLinkOld, Terrain tileLinkNew)
        {
            ActionId = 1;
            modifiedTerrainLink = tileLinkNew;
            originalDataStorage = new Terrain();
            originalDataStorage.copyDataFrom(tileLinkOld);
            newDataStorage = new Terrain();
            newDataStorage.copyDataFrom(tileLinkNew);
        }

        public override void Undo()
        {
            //throw new NotImplementedException();
            modifiedTerrainLink.copyDataFrom(originalDataStorage);
        }
        public override void Redo()
        {
            modifiedTerrainLink.copyDataFrom(newDataStorage);
        }
    }

    public class ActionChangeMountainProperty : Action
    {
        public Mountain modifiedMountainLink;
        public Mountain originalDataStorage;
        public Mountain newDataStorage;

        public ActionChangeMountainProperty(Mountain tileLinkOld, Mountain tileLinkNew)
        {
            ActionId = 2;
            modifiedMountainLink = tileLinkNew;
            originalDataStorage = new Mountain();
            originalDataStorage.copyDataFrom(tileLinkOld);
            newDataStorage = new Mountain();
            newDataStorage.copyDataFrom(tileLinkNew);
        }

        public override void Undo()
        {
            //throw new NotImplementedException();
            modifiedMountainLink.copyDataFrom(originalDataStorage);
        }
        public override void Redo()
        {
            modifiedMountainLink.copyDataFrom(newDataStorage);
        }
    }

    /*public class ActionChange<T> : Action
    {
        public T modifiedLink;
        public T originalDataStorage;
        public T newDataStorage;

        public ActionChangeMountainProperty(T tileLinkOld, T tileLinkNew)
        {
            ActionId = 2;
            modifiedLink = tileLinkNew;
            originalDataStorage = new T();
            originalDataStorage.copyDataFrom(tileLinkOld);
            newDataStorage = new T();
            newDataStorage.copyDataFrom(tileLinkNew);
        }

        public override void Undo()
        {
            //throw new NotImplementedException();
            modifiedLink.copyDataFrom(originalDataStorage);
        }
        public override void Redo()
        {
            modifiedLink.copyDataFrom(newDataStorage);
        }
    }*/

    public class PointerData
    {
        public PointF coord;
        public string srat;
        public int size;

        public PointerData(float[] coords, string srat, int size)
        {
            this.coord.X = coords[0];
            this.coord.Y = coords[1];
            this.srat = srat;
            this.size = size;
        }

        public PointerData(PointF coord, string srat, int size)
        {
            this.coord = coord;
            this.srat = srat;
            this.size = size;
        }
    }

    public class ChangePointer
    {
        //public PointF coord;
        //public string srat;
        public Pen SelectionPen = new Pen(Color.FromArgb(95, Color.Red), 50);
        //public List<Terrain> TerrainToMark;
        //public List<Mountain> MountainToMark;
        public List<PointerData>? DataToPoint; //? - allows null

        public void DrawChangePointers(Graphics g)
        {
            /*if (TerrainToMark != null)
            {
                foreach (Terrain t in TerrainToMark)
                {
                    float[] CoordPair = getCoordsByTerrain(t);
                    //if (CoordPair == null) Break;
                    var SelectionPen = new Pen(Color.FromArgb(95, Color.White), 50);
                    g.DrawRectangle(SelectionPen, CoordPair[0] - 5, CoordPair[1] - 5, 256 + 5, 256 + 5);
                }

            }*/

            if(DataToPoint!=null)
            {
                foreach (PointerData temp in DataToPoint)
                {
                    switch (temp.srat)
                    {
                        case "Terrain": g.DrawRectangle(SelectionPen, temp.coord.X - 5, temp.coord.Y - 5, 256 + 5, 256 + 5); break;
                        case "Mountain": g.DrawEllipse(SelectionPen, temp.coord.X - 5, temp.coord.Y - 5, 256 + 5, 256 + 5);  break;
                        default: break;
                    }
                }
            }
        }

        public void updateData(List<PointerData>? DataToPoint)
        {
            this.DataToPoint = DataToPoint;
        }

        public void clearData()
        {
            this.DataToPoint = null;
        }
    }
    
}
