using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;

// The class-sort overtaking fixture (--traffic-case class-sort): its grid must be exactly the
// declared class-mixed layout, and the classes must really differ in pace, or "the field sorted
// itself by class" would test nothing.
public static class ClassSortChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Class sort check failed: "+name);checks.Add(name);}
        int n=RaceGrid.ClassSortCars;
        var slots=Enumerable.Range(0,n).Select(RaceGrid.ClassSortSlot).ToArray();
        Check(slots.Distinct().Count()==n&&slots.All(s=>s>=0&&s<18&&s/3!=4),"15 distinct grid slots, three abreast, with row 5 left empty");
        string Row(int row)=>string.Join("-",Enumerable.Range(0,3).Select(c=>Array.IndexOf(slots,row*3+c)).Select(d=>d<0?"_":RaceGrid.ClassSortClass(d).ToString()));
        string layout=string.Join(" / ",Enumerable.Range(0,6).Select(Row));
        Check(layout=="3-3-3 / 2-2-2 / 3-3-3 / 1-1-1 / _-_-_ / 2-2-2","Grid rows front to back are "+layout);
        var roster=RaceRoster.Trial(n,2,1709,"cedar","stock",Array.Empty<string>(),"class-sort",null,false);
        Check(roster.Entries.All(e=>e.Slot==RaceGrid.ClassSortSlot(e.Id)&&e.EquipmentLabel==RaceGrid.ClassSortEquipment(e.Id)),"Trial roster places each class on its declared slot");
        Check(roster.Entries.Count(e=>e.EquipmentLabel=="club")==3&&roster.Entries.Count(e=>e.EquipmentLabel=="street")==6&&roster.Entries.Count(e=>e.EquipmentLabel=="stock")==6,"Three class-1 (club), six class-2 (street), six class-3 (stock) cars");
        var club=EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("club"));var street=EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("street"));var stock=EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("stock"));
        Check(club.AttainableSpeed>street.AttainableSpeed&&street.AttainableSpeed>stock.AttainableSpeed&&club.roadGrip>=street.roadGrip&&street.roadGrip>=stock.roadGrip,
            $"Classes are ordered in pace: top speed {club.AttainableSpeed:0.0} > {street.AttainableSpeed:0.0} > {stock.AttainableSpeed:0.0} m/s, grip {club.roadGrip:0.00} >= {street.roadGrip:0.00} >= {stock.roadGrip:0.00}");
        Check(RaceGrid.TrialSlot(0,24,"field")==RaceGrid.Slot(0,24),"Other fixtures keep their grid");
        return checks;
    }
}
